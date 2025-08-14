#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0050

using LibGit2Sharp;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.MongoDB;
using Microsoft.SemanticKernel.Embeddings;
using MongoDB.Driver;
using sk_multi_agent_system;
using sk_multi_agent_system.Models;


internal class Program
{
    static async Task Main(string[] args)
    {

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        //await IndexRepositoryAsync(configuration);

        Console.WriteLine("--- Multi-Agent Triage System Initializing ---");

        var triageSystem = new TriageSystem(configuration);

        Console.WriteLine("System Initialized. You can now chat with the TriageAgent.");
        Console.WriteLine("Type 'exit' to quit.");
        Console.WriteLine("----------------------------------------------------");

        while (true)
        {
            Console.Write("User > ");
            string userInput = Console.ReadLine() ?? "";

            if (string.IsNullOrWhiteSpace(userInput) || userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            await foreach (var message in triageSystem.RunAsync(userInput))
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(message);
                Console.ForegroundColor = originalColor;
            }
            Console.WriteLine();
        }

        Console.WriteLine("--- Session Ended ---");
    }


    private static async Task IndexRepositoryAsync(IConfiguration configuration)
    {
        Console.WriteLine("--- Starting Git Repository Indexing ---");

        // setup Kernel for embedding generation
        var kernel = Kernel.CreateBuilder()
            .AddOpenAIEmbeddingGenerator(
                configuration["OpenAI:EmbeddingModel"]!,
                configuration["OpenAI:ApiKey"]!
            ).Build();

        // connect to MongoDB and get a Vector Store instance directly
        var mongoClient = new MongoClient(configuration["MongoDB:ConnectionString"]!);
        var database = mongoClient.GetDatabase(configuration["MongoDB:DatabaseName"]!);
        var vectorStore = new MongoVectorStore(database);

        var collection = vectorStore.GetCollection<string, GitCommitRecord>(
            configuration["MongoDB:CollectionName"]!
        );

        var embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

        // Read the Git repo and save records
        using var repo = new Repository(configuration["Indexer:RepoPath"]!);
        int count = 0;
        var records = new List<GitCommitRecord>();
        foreach (var commit in repo.Commits)
        {

            var embedding = await embeddingGenerator.GenerateAsync(commit.Message);

            records.Add(new GitCommitRecord
            {
                CommitSha = commit.Sha,
                Message = commit.Message,
                Author = commit.Author.Name,
                Date = commit.Author.When.DateTime,
                Embedding = embedding.Vector 
            });
            count++;
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