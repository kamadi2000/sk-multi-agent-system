#pragma warning disable SKEXP0080
#pragma warning disable SKEXP0010

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using sk_multi_agent_system.Processes;
using Qdrant.Client;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appSettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var kernelBuilder = Kernel.CreateBuilder();

        kernelBuilder.AddOpenAIChatCompletion(
            configuration["OpenAI:ModelId"],
            configuration["OpenAI:ApiKey"]
        );

        kernelBuilder.AddOpenAIEmbeddingGenerator(
            configuration["OpenAI:EmbeddingModel"],
            configuration["OpenAI:ApiKey"],
            serviceId: "mongodb-vector-store"
        );

        kernelBuilder.AddOpenAIEmbeddingGenerator(
            configuration["QdrantVectorStore:TextEmbeddingModel"],
            configuration["OpenAI:ApiKey"],
            serviceId: "qdrant-vector-store"
        );
        
        var qdrantClient = new QdrantClient(
          host: configuration["QdrantVectorStore:Host"]!,
          https: true,
          apiKey: configuration["QdrantVectorStore:ApiKey"]!
        );

        kernelBuilder.Services.AddSingleton(_ => qdrantClient);
        kernelBuilder.Services.AddQdrantVectorStore();

        var kernel = kernelBuilder.Build();

        // Build the process
        var process = BugReportProcess.Build();

        // Start the process with a test bug report
        var bugReport = "When entering app credentials it does not appearing on the screen";
        //var result = await process.StartAsync(new { Start = bugReport });
        var result = await process.StartAsync(
            kernel,
            new KernelProcessEvent { Id = "Start", Data = bugReport }
        );

        if (result != null)
        {
            Console.WriteLine("Process Executed", result);
        }
        else
        {
            Console.WriteLine("Failed with no output");
        }
    }
}
