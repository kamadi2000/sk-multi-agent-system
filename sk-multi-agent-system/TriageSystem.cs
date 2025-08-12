#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0001

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
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

        // create specialist agent factories
        var codeIntelFactory = new CodeIntelAgent(kernel);
        var jiraFactory = new JiraAgent(kernel);
        var commFactory = new CommunicationAgent(kernel);

        // create the agents
        var codeIntelAgent = codeIntelFactory.Create();
        var jiraAgent = jiraFactory.Create();
        var commAgent = commFactory.Create();

        // Create the orchestrator
        var orchestrator = new TriageAgent(kernel);

        // Create the chat group
        _chat = new AgentGroupChat(codeIntelAgent, commAgent, jiraAgent)
        {
            // Set the rules for the chat group using the orchestrator's plan
            ExecutionSettings = orchestrator.CreateExecutionSettings([jiraAgent])
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