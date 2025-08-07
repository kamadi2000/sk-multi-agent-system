#pragma warning disable SKEXP0110

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using System.Diagnostics.CodeAnalysis;

namespace sk_multi_agent_system.Agents;

// This class is now the "manager" of specialist agents.
public class TriageAgent(Kernel kernel)
{
    private const string CodeIntelName = CodeIntelAgent.AgentName;
    private const string JiraAgentName = JiraAgent.AgentName;

    public AgentGroupChatSettings CreateExecutionSettings()
    {
        return new()
        {
            TerminationStrategy = CreateTerminationStrategy(),
            SelectionStrategy = CreateSelectionStrategy()
        };
    }

    private KernelFunctionSelectionStrategy CreateSelectionStrategy()
    {

        var selectionPrompt =
            """
            Your job is to determine which participant takes the next turn in a software triage conversation.
            State ONLY the name of the participant to take the next turn.

            Choose only from these participants:
            - {{{CodeIntelName}}}
            - {{{JiraAgentName}}}
            - User

            Always follow these rules:
            1. If the user asks a question about code, files, or commit history, it is {{{CodeIntelName}}}'s turn.
            2. If the user asks to create a bug report or ticket, it is {{{JiraAgentName}}}'s turn.
            3. If the {{{CodeIntelName}}} has provided information, the next turn is usually the User's to ask a follow-up question or the {{{JiraAgentName}}}'s if a bug needs to be filed.
            4. If the {{{JiraAgentName}}} has created a ticket, the conversation may be complete.
            5. Otherwise, it is the User's turn.

            History:
            {{$history}}

            Return only one name.
            """;

        return new KernelFunctionSelectionStrategy(
            KernelFunctionFactory.CreateFromPrompt(selectionPrompt), kernel)
        {
            HistoryVariableName = "history"
        };
    }

    private KernelFunctionTerminationStrategy CreateTerminationStrategy()
    {
        // This prompt decides when the entire conversation should end.
        var terminationPrompt =
            """
            Determine if the triage process is complete. The process is complete if:
            1. A Jira ticket has been successfully created.
            2. The user says "thank you", "done", or "goodbye".

            If the conversation is complete, respond with a single word: yes.

            History:
            {{$history}}
            """;

        return new KernelFunctionTerminationStrategy(
            KernelFunctionFactory.CreateFromPrompt(terminationPrompt), kernel)
        {
            ResultParser = result => result.GetValue<string>()?.Contains("yes", StringComparison.OrdinalIgnoreCase) ?? false,
            HistoryVariableName = "history"
        };
    }
}