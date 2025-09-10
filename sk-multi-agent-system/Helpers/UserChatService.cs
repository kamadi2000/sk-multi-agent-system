using Microsoft.SemanticKernel.Agents;
using sk_multi_agent_system.Agents;
#pragma warning disable SKEXP0110

public class UserChatService : IUserChatService
{
    private ChatCompletionAgent _bugAnalysisAgent;
    private ChatCompletionAgent _codeIntelAgent;
    private ChatCompletionAgent _commAgent;
    private ChatCompletionAgent _jiraAgent;
    private TriageAgent _orchestrator;

    private readonly Dictionary<string, AgentGroupChat> _userChats = new();

    // Called by TriageSystem after creating agents
    public void RegisterAgents(
        ChatCompletionAgent bugAnalysisAgent,
        ChatCompletionAgent codeIntelAgent,
        ChatCompletionAgent commAgent,
        ChatCompletionAgent jiraAgent,
        TriageAgent orchestrator)
    {
        _bugAnalysisAgent = bugAnalysisAgent;
        _codeIntelAgent = codeIntelAgent;
        _commAgent = commAgent;
        _jiraAgent = jiraAgent;
        _orchestrator = orchestrator;
    }

    public AgentGroupChat GetUserChat(string userId)
    {
        if (!_userChats.TryGetValue(userId, out var chat))
        {
            chat = new AgentGroupChat(_bugAnalysisAgent, _codeIntelAgent, _commAgent, _jiraAgent)
            {
                ExecutionSettings = _orchestrator.CreateExecutionSettings(new[] { _jiraAgent, _commAgent })
            };

            _userChats[userId] = chat;
        }

        return chat;
    }
}
