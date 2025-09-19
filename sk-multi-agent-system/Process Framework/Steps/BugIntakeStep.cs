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
    public BugIntakeStep() { }

    [KernelFunction]
    public async Task HandleBugReportAsync(Kernel kernel, string bugReport, KernelProcessStepContext context)
    {
        Console.WriteLine($"[{nameof(BugIntakeStep)}]: Received bug report. Checking for duplicates...");

        var similarBugs = await FindSimilarBugsAsync(kernel, bugReport);

        if (similarBugs.Any())
        {
            Console.WriteLine($"A similar bug has been already reported for this scenario. Terminating process");
            var outputMsg = $"This bug report appears to be a duplicate of an existing issue: {string.Join(", ", similarBugs)}";

            await context.EmitEventAsync("DuplicateFound", outputMsg);
        }
        else
        {
            Console.WriteLine($"[{nameof(BugIntakeStep)}]: New bug report. Continuing process.");


            await context.EmitEventAsync("BugReceived", bugReport);
        }
    }

    

    private async Task<IEnumerable<string>> FindSimilarBugsAsync(Kernel kernel, string bugReport)
    {
        var vectorStore = kernel.Services.GetRequiredService<VectorStore>()!;
        var embeddingGenerator = kernel.Services.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>("qdrant-vectore-store")!;

        var bugs = vectorStore.GetCollection<Guid, TriageAgentModel>("Bugs");
        var collections = vectorStore.ListCollectionNamesAsync();

        var collectionList = new HashSet<string>();

        var results = bugs.SearchAsync(bugReport, 5, new VectorSearchOptions<TriageAgentModel>
        {
            VectorProperty = bug => bug.DescriptionEmbedding,
            IncludeVectors = true
        });

        var searchedResult = new HashSet<string>();
        await foreach (var result in results)
        {
            if (result.Score >= 0.7)
            {
                searchedResult.Add($"{result.Record.Bug_Description}");
            }
        }

        // Search for similar bugs with a score threshold
        var results = await vectorStore.GetNearestMatchesAsync(
            collectionName: "Bugs",
            embedding: await embeddingGenerator.GenerateEmbeddingAsync(bugReport),
            limit: 5,
            minRelevanceScore: 0.7
        );

        return results.Select(r => r.Metadata.Text);
    }


}
