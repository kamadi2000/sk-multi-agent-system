#pragma warning disable SKEXP0001

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.MongoDB;
using MongoDB.Driver;
using sk_multi_agent_system.Models;
using System.ComponentModel;
using System.Text;

namespace sk_multi_agent_system.Plugins;

public class GitPlugin
{
    private readonly IConfiguration _configuration;
    private readonly Kernel _kernel;

    public GitPlugin(IConfiguration configuration, Kernel kernel)
    {
        _configuration = configuration;
        _kernel = kernel;
    }


    [KernelFunction, Description("Performs a semantic search on commit history, with optional filters for author and date.")]
    public async Task<string> SemanticSearchCommits(
        [Description("The user's question or topic to search for.")] string query
    )
    {
        try
        {
            // Connect to MongoDB
            var mongoClient = new MongoClient(_configuration["MongoDB:ConnectionString"]!);
            var database = mongoClient.GetDatabase(_configuration["MongoDB:DatabaseName"]!);
            var vectorStore = new MongoVectorStore(database);
            var collection = vectorStore.GetCollection<string, GitCommitRecord>(
                _configuration["MongoDB:CollectionName"]!
            );

            // Generate an embedding for the user's search query
            var embeddingGenerator = _kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
            Embedding<float> searchEmbedding = await embeddingGenerator.GenerateAsync(query);

            // perform the search using the vector and options
            var results = await collection.SearchAsync(searchEmbedding.Vector, top: 5).ToListAsync();

            if (!results.Any())
            {
                return "I couldn't find any commits that matched your query.";
            }

            // Format and return the results
            var output = new StringBuilder();
            output.AppendLine("Here are the most relevant commits I found:");
            foreach (var result in results)
            {
                if (result.Score < 0.5) continue; // Manually filter by relevance score

                output.AppendLine($"  - (Relevance: {result.Score:P0}) Commit: {result.Record.CommitSha.Substring(0, 7)} by {result.Record.Author}");
                output.AppendLine($"    Message: {result.Record.Message.Split('\n').FirstOrDefault()}");
            }

            return output.ToString();
        }
        catch (Exception ex)
        {
            return $"An error occurred during the search: {ex.Message}";
        }
    }
}