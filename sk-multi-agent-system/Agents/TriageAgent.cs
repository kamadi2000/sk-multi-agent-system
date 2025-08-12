#pragma warning disable SKEXP0110

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Diagnostics.CodeAnalysis;

namespace sk_multi_agent_system.Agents;

// This class is the "manager" of specialist agents.
public class TriageAgent(Kernel kernel)
{
    private const string CodeIntelName = CodeIntelAgent.AgentName;
    private const string JiraAgentName = JiraAgent.AgentName;
    private const string CommAgentName = CommunicationAgent.AgentName;

    public AgentGroupChatSettings CreateExecutionSettings(ChatCompletionAgent[] agents)
    {
        return new()
        {
            TerminationStrategy = CreateTerminationStrategy(agents),
            SelectionStrategy = CreateSelectionStrategy()
        };
    }

    private KernelFunctionSelectionStrategy CreateSelectionStrategy()
    {

        var selectionPrompt =
            $$$"""
            You are a highly intelligent routing agent. Your job is to determine which participant takes the next turn in a software triage conversation based on a strict set of rules and the conversation history.
            State ONLY the name of the participant to take the next turn.

            # PARTICIPANTS
            - {{{CodeIntelName}}}: An expert at analyzing code, commits, files, and git history.
            - {{{CommAgentName}}}: An expert at communicating with others user and groups.
            - {{{JiraAgentName}}}: An expert at creating and managing Jira tickets.

            # RULES
            1.  Initial Query: If the user asks a new question about code, files, or git history, choose {{{CodeIntelName}}}.
        
            2.  Context Persistence: If {{{CodeIntelName}}} was the last speaker and the user asks a direct follow-up question about the same topic, choose {{{CodeIntelName}}} again.

            3. Handoff to Communication: If the user requests to send a message to the people mentioned in the message given by {{{CodeIntelName}}}, then it's {{{CommAgentName}}} turn.
        
            4.  Handoff to Jira: If {{{CodeIntelName}}} has just provided analysis and the user's response indicates they are now ready to file a bug report e bug, YOU MUST choose {{{JiraAgentName}}}.
        
            5.  Jira Task: If the user's initial request is clearly to create a ticket or bug report, choose {{{JiraAgentName}}}.

            6.  Always select one of the participants listed above.

            # CONVERSATION HISTORY
            {{$history}}

            Based on the LAST message in the history and the rules, who speaks next? Return only one name.
            """;

        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        return new KernelFunctionSelectionStrategy(
            KernelFunctionFactory.CreateFromPrompt(selectionPrompt), kernel)
        {
            HistoryVariableName = "history",
            HistoryReducer = new ChatHistorySummarizationReducer(chatCompletionService, 5) // function that summerize the hsitroy and keep last 5 as raw

        };
    }

    private KernelFunctionTerminationStrategy CreateTerminationStrategy(ChatCompletionAgent[] agents)
    {
        // This prompt decides when the entire conversation should end.
        var terminateFunction = KernelFunctionFactory.CreateFromPrompt(
            """
            Determine if the triage process is complete. The process is complete if:
            1. A Jira ticket has been successfully created.
            2. The user says "thank you", "done", or "goodbye".

            If the conversation is complete, respond with a single word: yes.

            History:
            {{$history}}
            """
            );

        return new KernelFunctionTerminationStrategy(terminateFunction, kernel)
        {
            Agents = agents,
            ResultParser = result => result.GetValue<string>()?.Contains("yes", StringComparison.OrdinalIgnoreCase) ?? false,
            HistoryVariableName = "history",
            MaximumIterations = 1,
        };
    }
}