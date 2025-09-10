using Atlassian.Jira;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Qdrant.Client;
using sk_multi_agent_system.Agents;
using sk_multi_agent_system.Plugins;
using Telegram.Bot;
#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010

public class TriageSystem
{
    // Instead of one chat, keep a dictionary of chats per userId
    private readonly Dictionary<string, AgentGroupChat> _userChats = new();

    private readonly Kernel _agentKernel;
    private readonly TriageAgent _orchestrator;

    private readonly IUserChatService _chatService;

    public TriageSystem(IConfiguration configuration, ITelegramBotClient botClient, IUserChatService chatService)
    {
        _chatService = chatService;

        // Create base kernel
        var baseKernelBuilder = Kernel.CreateBuilder();
        baseKernelBuilder.AddOpenAIChatCompletion(
            configuration["OpenAI:ModelId"]!,
            configuration["OpenAI:ApiKey"]!
        );

        baseKernelBuilder.AddOpenAIEmbeddingGenerator(
            configuration["QdrantVectorStore:TextEmbeddingModel"]!,
            configuration["OpenAI:ApiKey"]!,
            serviceId: "qdrant-vectore-store"
        );

        baseKernelBuilder.AddOpenAIEmbeddingGenerator(
            configuration["OpenAI:EmbeddingModel"]!,
            configuration["OpenAI:ApiKey"]!,
            serviceId: "mongodb-vector-store"
        );

        var qdrantClient = new QdrantClient(
            host: configuration["QdrantVectorStore:Host"]!,
            https: true,
            apiKey: configuration["QdrantVectorStore:ApiKey"]!
        );

        baseKernelBuilder.Services.AddSingleton(_ => qdrantClient);
        baseKernelBuilder.Services.AddQdrantVectorStore();

        var baseKernel = baseKernelBuilder.Build();
        var gitPlugin = new GitPlugin(configuration, baseKernel);
        var jiraPlugin = new JiraPlugin(configuration["Jira:Url"]!, configuration["Jira:Username"]!, configuration["Jira:ApiToken"]!);
        var commPlugin = new CommPlugin(botClient, _chatService);
        var bugStorePlugin = new BugStorePlugin(baseKernel, qdrantClient);

        var agentKernel = baseKernel.Clone();
        agentKernel.Plugins.AddFromObject(gitPlugin);
        agentKernel.Plugins.AddFromObject(jiraPlugin);
        agentKernel.Plugins.AddFromObject(commPlugin);
        agentKernel.Plugins.AddFromObject(bugStorePlugin);

        // Agents created once (can be reused across multiple group chats)
        var _codeIntelAgent = new CodeIntelAgent(agentKernel).Create();
        var _jiraAgent = new JiraAgent(agentKernel).Create();
        var _commAgent = new CommunicationAgent(agentKernel).Create();
        var _bugAnalysisAgent = new BugAnalysisAgent(agentKernel).Create();

        // Orchestrator
        _orchestrator = new TriageAgent(agentKernel);
        _agentKernel = agentKernel;

         _chatService.RegisterAgents(_bugAnalysisAgent, _codeIntelAgent, _commAgent, _jiraAgent, _orchestrator);
    }

    // Run conversation for a specific user
    public async IAsyncEnumerable<string> RunAsync(string userId, string userMessage)
    {
        var chat = _chatService.GetUserChat(userId);

        var assisstantMessage = "";

        //Saving the user message
        chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, userMessage));

        await foreach (var message in chat.InvokeAsync())
        {
            assisstantMessage += message.Content;
            yield return $"{message.AuthorName}: {message.Content}";
        }

        chat.AddChatMessage(new ChatMessageContent(AuthorRole.Assistant, assisstantMessage));
    }
}