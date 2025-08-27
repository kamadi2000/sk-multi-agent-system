#pragma warning disable SKEXP0110

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;

namespace sk_multi_agent_system.Agents;

public class BugAnalysisAgent
{
    public const string AgentName = "BugAnalysisAgent";
    private  readonly Kernel _kernel;
    public const string AgentInstructions =
        """
        You are an expert bug analysis agent. 

        Your responsibilties are:

        1. When the user reports a bug, first use the 'search_bug' function to check 
        if a similar bug exists. Pass the user message to this function as a parameter.
        2. If a match is found, inform the user and ask if they want to reuse the existing 
        bug or create a new one.
        3. If the user wants a new bug, call SaveBugAsync function to save it in the 
        database, then forward it to CodeIntelAgent. Pass the user message, chat_id and 
        user_id to this function as parameters.
        4. If no match is found, directly call 'save_bug' and then forward it.
        5. Politely decline to answer if the query is not related to bug triage.
        """;

    public BugAnalysisAgent(Kernel kernel)
    {
        _kernel = kernel;
    }

    public ChatCompletionAgent Create()
    {
        return new ChatCompletionAgent
        {
            Name = AgentName,
            Instructions = AgentInstructions,
            Kernel = _kernel,
            Arguments = new KernelArguments(new PromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            })
        };
    }
}
