using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using sk_multi_agent_system.Plugins;
using Telegram.Bot;

namespace sk_multi_agent_system.Agents;

public class CommunicationAgent(Kernel _kernel)
{
    public const string AgentName = "CommunicationAgent";
    public const string AgentInstructions =
        """
        You are a communication assistant.
        When given a message from the CodeIntelAgent, identify any mentioned usernames
        and send them a relevant message using the send_message_to_user function.
        Only call the function if a valid username is detected.
        """;

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
