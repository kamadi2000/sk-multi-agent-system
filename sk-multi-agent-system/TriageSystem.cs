#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
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
        var builder = Kernel.CreateBuilder().AddOpenAIChatCompletion(
            configuration["OpenAI:ModelId"]!,
            configuration["OpenAI:ApiKey"]!
            );

        builder.AddOpenAIEmbeddingGenerator(
            configuration["QdrantVectorStore:TextEmbeddingModel"]!,
            configuration["OpenAI:ApiKey"]!
            );

        var qdrantClient = new QdrantClient(
          host: configuration["QdrantVectorStore:Host"]!,
          https: true,
          apiKey: configuration["QdrantVectorStore:ApiKey"]!
        );

        builder.Services.AddSingleton(_ => qdrantClient);
        builder.Services.AddQdrantVectorStore();

        var gitPlugin = new GitPlugin(
            configuration["Git:RepoPath"]!
        );

        var jiraPlugin = new JiraPlugin(
            configuration["Jira:Url"]!,
            configuration["Jira:Username"]!,
            configuration["Jira:ApiToken"]!
        );

        var commPlugin = new CommPlugin(botClient);

        builder.Plugins.AddFromObject(gitPlugin);
        builder.Plugins.AddFromObject(jiraPlugin);
        builder.Plugins.AddFromObject(commPlugin);
        var kernel = builder.Build();

        var bugStorePlugin = new BugStorePlugin(kernel, qdrantClient);
        kernel.Plugins.AddFromObject(bugStorePlugin);

        // create specialist agent factories
        var codeIntelFactory = new CodeIntelAgent(kernel);
        var jiraFactory = new JiraAgent(kernel);
        var commFactory = new CommunicationAgent(kernel);
        var bugStoreFactory = new BugAnalysisAgent(kernel);

        // create the agents
        var codeIntelAgent = codeIntelFactory.Create();
        var jiraAgent = jiraFactory.Create();
        var commAgent = commFactory.Create();
        var bugAnalysisAgent = bugStoreFactory.Create();

        // Create the orchestrator
        var orchestrator = new TriageAgent(kernel);

        // Create the chat group
        _chat = new AgentGroupChat(bugAnalysisAgent, codeIntelAgent, commAgent, jiraAgent)
        {
            // Set the rules for the chat group using the orchestrator's plan
            ExecutionSettings = orchestrator.CreateExecutionSettings([jiraAgent, commAgent])
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