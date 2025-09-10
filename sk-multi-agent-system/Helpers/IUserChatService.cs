using Microsoft.SemanticKernel.Agents;
using sk_multi_agent_system.Agents;
#pragma warning disable SKEXP0110

public interface IUserChatService
{
    AgentGroupChat GetUserChat(string userId);

    void RegisterAgents(ChatCompletionAgent bugAnalysisAgent,
        ChatCompletionAgent codeIntelAgent,
        ChatCompletionAgent commAgent,
        ChatCompletionAgent jiraAgent,
        TriageAgent orchestrator);
}
