using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.ComponentModel;
using Telegram.Bot.Types;

namespace sk_multi_agent_system.Plugins;

public class BugStorePlugin
{
    private readonly Kernel _kernel;
    private readonly QdrantClient _qdrant;

    public BugStorePlugin(Kernel kernel, QdrantClient qdrant)
    {
        _kernel = kernel;
        _qdrant = qdrant;
    }

    [KernelFunction("search_bug")]
    [Description("Search for a bug in the Qdrant vector database using semantic similarity.")]
    public async Task<HashSet<string>> SearchBugAsync(
        [Description("Bug description to search for.")] string query)
    {
        var vectorStore = _kernel.Services.GetRequiredService<VectorStore>()!;
        var embeddingGenerator = _kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>("qdrant-vectore-store")!;
        var queryEmbedding = await embeddingGenerator.GenerateVectorAsync(query);

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

        var results = bugs.SearchAsync(queryEmbedding, 5, new VectorSearchOptions<TriageAgentModel>
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

        if (searchedResult.Count > 0)
        {
            return searchedResult;
        }
        return new HashSet<string>();
    }

    [KernelFunction("save_bug")]
    [Description("Save a new bug report into the Qdrant vector database.")]
    public async Task<string> SaveBugAsync(
        [Description("Bug description to save.")] string description
        //[Description("Chat Id of telegram user which reports the bug.")] string chatId,
        //[Description("User Id of telegram user which reports the bug.")] string userId
        )
    {
        var vectorStore = _kernel.Services.GetRequiredService<VectorStore>()!;
        var embeddingGenerator = _kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>("qdrant-vectore-store")!;
        var queryEmbedding = await embeddingGenerator.GenerateVectorAsync(description);

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
            Bug_Description = description,
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
