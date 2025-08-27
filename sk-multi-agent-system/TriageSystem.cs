#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Qdrant.Client;
using sk_multi_agent_system.Agents;
using sk_multi_agent_system.Plugins;
using System.Diagnostics.CodeAnalysis;
using Telegram.Bot;

namespace sk_multi_agent_system;

public class TriageSystem
{
    private readonly AgentGroupChat _chat;

    public TriageSystem(IConfiguration configuration, ITelegramBotClient botClient)
    {
        // Create a single, shared Kernel
        var baseKernelBuilder = Kernel.CreateBuilder();
        baseKernelBuilder.AddOpenAIChatCompletion(
            configuration["OpenAI:ModelId"]!,
            configuration["OpenAI:ApiKey"]!
            );

        baseKernelBuilder.AddOpenAIEmbeddingGenerator(
            configuration["QdrantVectorStore:TextEmbeddingModel"]!,
            configuration["OpenAI:ApiKey"]!
            serviceId: "qdrant-vectore-store"
            );

        baseKernelBuilder.AddOpenAIEmbeddingGenerator(
            configuration["OpenAI:EmbeddingModel"]!,
            configuration["OpenAI:ApiKey"]!
            serviceId: "mongodb-vector-store"
        );

        var qdrantClient = new QdrantClient(
          host: configuration["QdrantVectorStore:Host"]!,
          https: true,
          apiKey: configuration["QdrantVectorStore:ApiKey"]!
        );

        baseKernelBuilder.Services.AddSingleton(_ => qdrantClient);
        baseKernelBuilder.Services.AddQdrantVectorStore(serviceId: "qdrant-vectore-store");

        var baseKernel = baseKernelBuilder.Build();
        var gitPlugin = new GitPlugin(configuration, baseKernel);
        var jiraPlugin = new JiraPlugin(
            configuration["Jira:Url"]!,
            configuration["Jira:Username"]!,
            configuration["Jira:ApiToken"]!
        );
        var commPlugin = new CommPlugin(botClient);
        var bugStorePlugin = new BugStorePlugin(baseKernel, qdrantClient);

        var agentKernel = baseKernel.Clone();
        
        agentKernel.Plugins.AddFromObject(gitPlugin);
        agentKernel.Plugins.AddFromObject(jiraPlugin);
        agentKernel.Plugins.AddFromObject(commPlugin);
        agentKernel.Plugins.AddFromObject(bugStorePlugin);
                
        // create specialist agent factories
        var codeIntelFactory = new CodeIntelAgent(agentKernel);
        var jiraFactory = new JiraAgent(agentKernel);
        var commFactory = new CommunicationAgent(agentKernel);
        var bugStoreFactory = new BugAnalysisAgent(agentKernel);

        // create the agents
        var codeIntelAgent = codeIntelFactory.Create();
        var jiraAgent = jiraFactory.Create();
        var commAgent = commFactory.Create();
        var bugAnalysisAgent = bugStoreFactory.Create();

        // Create the orchestrator
        var orchestrator = new TriageAgent(agentKernel);

        // Create the chat group
        _chat = new AgentGroupChat(bugAnalysisAgent, codeIntelAgent, commAgent, jiraAgent)
        {
            // Set the rules for the chat group using the orchestrator's plan
<<<<<<< HEAD
            ExecutionSettings = orchestrator.CreateExecutionSettings([jiraAgent, commAgent])
=======

>>>>>>> origin/groupchat-orchestration
        };
    }

    public async IAsyncEnumerable<string> RunAsync(string userMessage)
    {
        // Add the initial user message
        _chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, userMessage));

        await foreach (var message in _chat.InvokeAsync())
        {
            yield return $"{message.AuthorName}: {message.Content}";
        }
    }
}