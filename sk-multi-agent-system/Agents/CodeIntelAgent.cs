using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using sk_multi_agent_system.Plugins;

namespace sk_multi_agent_system.Agents;
public class CodeIntelAgent(Kernel kernel)
{
    public const string AgentName = "CodeIntelAgent";
    public const string AgentInstructions =
        """
        You are an expert in code repository analysis. 
        Your goal is to help users by first understanding their needs.
        When a user mentions a file, first acknowledge the file.
        Then, ASK them what specific information they want about it.
        Only after the user specifies what they want, use the appropriate tool to answer their question.
        If you can't perforom the requested task, tell the user that you don't have necassary supported tools for that.
        Be precise and provide only the information requested.
        """;

    /// Creates a new ChatCompletionAgent specialized for code intelligence tasks.
    /// A new ChatCompletionAgent instance.
    public ChatCompletionAgent Create()
    {
        // This agent is configured to automatically use any function within GitPlugin.
        // It will intelligently choose between FindFileAndGetHistory and ListAllTrackedFiles
        // based on the user's prompt.
        return new ChatCompletionAgent
        {
            Name = AgentName,
            Instructions = AgentInstructions,
            Kernel = kernel,
            Arguments = new KernelArguments(new PromptExecutionSettings()
            {
                // use Auto so the agent can pick the best tool from the plugin.
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            })
        };
    }
}