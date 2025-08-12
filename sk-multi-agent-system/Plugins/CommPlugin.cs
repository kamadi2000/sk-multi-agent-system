using Microsoft.SemanticKernel;
using System.ComponentModel;
using Telegram.Bot;

namespace sk_multi_agent_system.Plugins;

public class CommPlugin
{
    private readonly ITelegramBotClient _botClient;

    public CommPlugin(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    [KernelFunction("send_message_to_user")]
    [Description("Send a Telegram message to a specific username.")]
    public async Task<string> SendMessageAsync(
        [Description("The username of the person to send the message to")] string username,
        [Description("The message content to send")] string message)
    {
        var user = UserDatabase.GetUsers().FirstOrDefault(u =>
            string.Equals(u.TelegramUsername, username, StringComparison.OrdinalIgnoreCase));

        if (user == null)
        {
            return $"User '{username}' not found.";
        }

        await _botClient.SendMessage(
            chatId: user.UserId,
            text: message
        );

        return $"Message sent to {username}.";
    }
}
