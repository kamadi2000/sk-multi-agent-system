using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.ComponentModel;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace sk_multi_agent_system.Plugins;

public class CommPlugin
{
    private readonly ITelegramBotClient _botClient;
    private readonly IUserChatService _chatService;

    public CommPlugin(ITelegramBotClient botClient, IUserChatService chatService)
    {
        _botClient = botClient;
        _chatService = chatService;
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

        var chat = _chatService.GetUserChat(user.UserId.ToString());
        chat.AddChatMessage(new ChatMessageContent(AuthorRole.Assistant, message));

        return $"Message sent to {username}.";
    }
}
