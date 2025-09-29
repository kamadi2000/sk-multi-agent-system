#pragma warning disable SKEXP0080


using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Qdrant.Client.Grpc;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;

using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;

using Telegram.Bot.Types;


namespace sk_multi_agent_system.Steps;

public class BugIntakeStep : KernelProcessStep
{

    [KernelFunction]
    public async Task<string> HandleBugReportAsync(Kernel kernel, string bugReport, KernelProcessStepContext context)
    {
        Console.WriteLine($"[{nameof(BugIntakeStep)}]: Received bug report. Checking for duplicates...");

        var similarBugs = await FindSimilarBugsAsync(kernel, bugReport);

        if (similarBugs.Any())
        {
            Console.WriteLine($"A similar bug has been already reported for this scenario. Terminating process");
            var outputMsg = $"This bug report appears to be a duplicate of an existing issue: {string.Join(", ", similarBugs)}";

            await context.EmitEventAsync("DuplicateFound", outputMsg);

            await context.EmitEventAsync("HumanVerificationNeeded", outputMsg );

            return outputMsg;
        }
        else
        {
            Console.WriteLine($"[{nameof(BugIntakeStep)}]: New bug report. Continuing process.");

            await SaveBugAsync(kernel, bugReport);
            await context.EmitEventAsync("BugReceived", bugReport);
            return bugReport;
        }
    }



    private async Task<IEnumerable<string>> FindSimilarBugsAsync(Kernel kernel, string bugReport)
    {
        var vectorStore = kernel.Services.GetRequiredService<VectorStore>()!;
        var embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>("qdrant-vector-store");
        var queryEmbedding = await embeddingGenerator.GenerateVectorAsync(bugReport);

        var bugs = vectorStore.GetCollection<Guid, TriageAgentModel>("Bugs");
        var collections = vectorStore.ListCollectionNamesAsync();

        var collectionList = new HashSet<string>();

        await foreach (var collection in collections)
        {
            collectionList.Add(collection);
        }

        var collectionExists = collectionList.Contains("Bugs");

        if (!collectionExists)
        {
            return null;
        }

        var results = bugs.SearchAsync(queryEmbedding, 3, new VectorSearchOptions<TriageAgentModel>
        {
            VectorProperty = bug => bug.DescriptionEmbedding,
            IncludeVectors = true
        });

        var searchedResult = new HashSet<string>();
        await foreach (var result in results)
        {
            if (result.Score >= 0.85)
            {
                searchedResult.Add($"{result.Record.Bug_Description}");
            }
        }

        if (searchedResult.Count > 0)
        {
            return searchedResult;
        }
        return new HashSet<string>();
    }

    private async Task<string> SaveBugAsync(Kernel kernel, string bugReport
        )
    {
        var vectorStore = kernel.Services.GetRequiredService<VectorStore>()!;
        var embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>("qdrant-vector-store");
        var queryEmbedding = await embeddingGenerator.GenerateVectorAsync(bugReport);

        var bugs = vectorStore.GetCollection<Guid, TriageAgentModel>("Bugs");

        var collections = vectorStore.ListCollectionNamesAsync();

        var collectionList = new HashSet<string>();

        await foreach (var collection in collections)
        {
            collectionList.Add(collection);
        }

        var collectionExists = collectionList.Contains("Bugs");

        if (!collectionExists)
        {
            return null;
        }

        var newBug = new TriageAgentModel
        {
            Key = Guid.NewGuid(),
            UserID = "1234",
            ChatID = "1234",
            Bug_Description = bugReport,
            DescriptionEmbedding = queryEmbedding
        };

        try
        {
            await bugs.UpsertAsync(newBug);
        }
        catch (Exception ex)
        {
            return $"Failed to save bug: {ex.Message}";
        }

        return "Bug saved successfully.";
    }
}
