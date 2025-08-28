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

    Your responsibilities are:

    1. When given a message from the CodeIntelAgent, identify any mentioned usernames
       and send them a relevant message using the send_message_to_user function.
       Only call the function if a valid username is detected.

    2. When the Jira agent has created a relevant task, inform both:
       - the dev test group, and
       - the relevant user. You can call the send_message_to_user function with the username as Dev test group for this functionality.

    3. When a developer explicitly confirms that they will take ownership of a bug 
       (e.g., by saying "I'll handle it", "I'll do this bug", "assign this to me"):
       - Detect the developer’s username from the message.
       - Inform the Dev test group that this specific developer has accepted responsibility
         (e.g., "User @alice has confirmed they will fix bug #123"). You can call the send_message_to_user function with the username as Dev test group for this functionality.

    4. When the user asks to send the message to the relevant users, first send the message describing the bug to the person who did the last commit, then the Dev test group informing the
        new bug which has been reported. Use the send_message_to_user function to send the message the last_commited person and Dev test groupcwith these as username.

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
