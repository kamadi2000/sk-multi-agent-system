#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using sk_multi_agent_system.Agents;
using sk_multi_agent_system.Plugins;

namespace sk_multi_agent_system;

public class TriageSystem
{
    private readonly AgentGroupChat _chat;

    public TriageSystem(IConfiguration configuration)
    {
        // Create a single, shared Kernel
        var baseKernelBuilder = Kernel.CreateBuilder();
        baseKernelBuilder.AddOpenAIChatCompletion(
            configuration["OpenAI:ModelId"]!,
            configuration["OpenAI:ApiKey"]!
            );

        baseKernelBuilder.AddOpenAIEmbeddingGenerator(
            configuration["OpenAI:EmbeddingModel"]!,
            configuration["OpenAI:ApiKey"]!
        );
        var baseKernel = baseKernelBuilder.Build();
        var gitPlugin = new GitPlugin(configuration, baseKernel);
        var jiraPlugin = new JiraPlugin(
            configuration["Jira:Url"]!,
            configuration["Jira:Username"]!,
            configuration["Jira:ApiToken"]!
        );
        var agentKernel = baseKernel.Clone();

        agentKernel.Plugins.AddFromObject(gitPlugin);
        agentKernel.Plugins.AddFromObject(jiraPlugin);
        
        // create specialist agent factories
        var codeIntelFactory = new CodeIntelAgent(agentKernel);
        var jiraFactory = new JiraAgent(agentKernel);

        // create the agents
        var codeIntelAgent = codeIntelFactory.Create();
        var jiraAgent = jiraFactory.Create();

        // Create the orchestrator
        var orchestrator = new TriageAgent(agentKernel);

        // Create the chat group
        _chat = new AgentGroupChat(codeIntelAgent, jiraAgent)
        {
            // Set the rules for the chat group using the orchestrator's plan
            ExecutionSettings = orchestrator.CreateExecutionSettings([])
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