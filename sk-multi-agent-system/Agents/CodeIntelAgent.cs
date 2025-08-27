using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using sk_multi_agent_system.Plugins;

namespace sk_multi_agent_system.Agents;
public class CodeIntelAgent(Kernel kernel)
{
    public const string AgentName = "CodeIntelAgent";
    public const string AgentInstructions =
        """
        You are an expert in analyzing software repositories and bug reports.

        When the user provides a bug report:
        1. Carefully read and summarize the issue in your own words.
        2. Identify any file names, functions, or components mentioned.
        3. Use the semantic search plugin to find the most relevant commits that modified those areas.
        4. For the top relevant commit(s), return the following information:
           - File name(s) affected
           - Latest commit touching the file (SHA, message, author, date)
           - The original author of the file (first commit touching it)
           - A short summary of what the commit changed (from the description field in embeddings, if available)

        If the bug report does not specify a file, use semantic search to infer the most relevant code area.
        If you cannot find relevant commits, politely say so.
        
        Be precise, concise, and provide only the requested technical information in a structured way.
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