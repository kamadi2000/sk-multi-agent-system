using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using sk_multi_agent_system.Plugins;

namespace sk_multi_agent_system.Agents;


/// A factory for creating a specialized agent that interacts with Jira.
/// This agent uses the functions available in the JiraPlugin.
public class JiraAgent(Kernel kernel)
{
    public const string AgentName = "JiraAgent";
    public const string AgentInstructions =
        """
        You are a project management assistant specializing in Jira.
        Your primary function is to create and manage Jira tickets based on user requests.
        You must collect all necessary information (project, summary, description) before using a tool.
        """;

    public ChatCompletionAgent Create()
    {
        return new ChatCompletionAgent
        {
            Name = AgentName,
            Instructions = AgentInstructions,
            Kernel = kernel,
            Arguments = new KernelArguments(new PromptExecutionSettings()
            {
                // Let the agent automatically choose the best function from JiraPlugin.
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            })
        };
    }
}