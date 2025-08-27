#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0050

using LibGit2Sharp;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.MongoDB;
using MongoDB.Driver;
using sk_multi_agent_system;
using sk_multi_agent_system.Models;
using System.Text;
using System.Threading.Tasks;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        try
        {
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            // This will catch *startup* exceptions
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FATAL] Application failed to start: {ex}");
            Console.ResetColor();
            throw; // rethrow so the host still knows it crashed
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                var env = hostingContext.HostingEnvironment;

                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                });

                // Register your services
                services.AddSingleton<TelegramBotService>();
                services.AddHostedService<TelegramBotWorker>();
            });
    
    private static async Task IndexRepositoryAsync(IConfiguration configuration)
    {
        Console.WriteLine("--- Starting Git Repository Indexing ---");

        // setup Kernel for chat completion and embedding generation
        var kernelBuilder = Kernel.CreateBuilder()
        .AddOpenAIChatCompletion(
            configuration["OpenAI:ModelId"]!,
            configuration["OpenAI:ApiKey"]!
        )
        .AddOpenAIEmbeddingGenerator(
            configuration["OpenAI:EmbeddingModel"]!,
            configuration["OpenAI:ApiKey"]!
        );

        var kernel = kernelBuilder.Build();

        // connect to MongoDB and get a Vector Store instance directly
        var mongoClient = new MongoClient(configuration["MongoDB:ConnectionString"]!);
        var database = mongoClient.GetDatabase(configuration["MongoDB:DatabaseName"]!);
        var vectorStore = new MongoVectorStore(database);

        var collection = vectorStore.GetCollection<string, GitCommitRecord>(
            configuration["MongoDB:CollectionName"]!
        );

        var embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        // Read the Git repo and save records
        using var repo = new Repository(configuration["Indexer:RepoPath"]!);
        var records = new List<GitCommitRecord>();

        foreach (var commit in repo.Commits)
        {
            var richTextBuilder = new StringBuilder();

            richTextBuilder.AppendLine($"Commit Message: {commit.Message}");
            richTextBuilder.AppendLine($"Author: {commit.Author.Name}");
            richTextBuilder.AppendLine($"Date: {commit.Author.When.DateTime}");

            if (commit.Parents.Any())
            {
                var parent = commit.Parents.First();
                var patch = repo.Diff.Compare<Patch>(parent.Tree, commit.Tree);

                richTextBuilder.AppendLine("\nChanged Files and Diff:");
                richTextBuilder.AppendLine(patch.Content);
            }

            string commitContext = richTextBuilder.ToString();

            var contextPrompt = $@"
            You are an AI assistant analyzing git commits.
            Summarize the commit below in a clear, concise description of code changes:

            {commitContext}
            ";

            var chatResult = await chatService.GetChatMessageContentAsync(
            new ChatHistory
            {
                new ChatMessageContent(AuthorRole.System, "You are a software assistant that explains git commits."),
                new ChatMessageContent(AuthorRole.User, commitContext)
            }
            );

            if (string.IsNullOrWhiteSpace(chatResult?.Content))
            {
                Console.WriteLine($"Skipping commit {commit.Sha}, no description generated.");
                continue;
            }

            string description = chatResult.Content;

            Embedding<float> embedding = await embeddingGenerator.GenerateAsync(description);

            records.Add(new GitCommitRecord
            {
                CommitSha = commit.Sha,
                Message = commit.Message,
                Author = commit.Author.Name,
                Date = commit.Author.When.DateTime,
                Description = description,
                Embedding = embedding.Vector
            });
        }

        var nativeCollection = database.GetCollection<GitCommitRecord>(
            configuration["MongoDB:CollectionName"]!
        );

        var bulkOps = new List<WriteModel<GitCommitRecord>>();
        foreach (var record in records)
        {
            var filter = Builders<GitCommitRecord>.Filter.Eq(r => r.CommitSha, record.CommitSha);
            bulkOps.Add(new ReplaceOneModel<GitCommitRecord>(filter, record) { IsUpsert = true });
        }

        if (bulkOps.Any())
        {
            await nativeCollection.BulkWriteAsync(bulkOps);
        }

        Console.WriteLine($"---Indexing Complete: {records.Count} commits saved to MongoDB ---");
    }
}
