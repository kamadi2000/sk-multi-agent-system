using Microsoft.Extensions.Configuration;
using sk_multi_agent_system;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

public class TelegramBotService
{
    private readonly ITelegramBotClient _botClient;
    private readonly TriageSystem _triageSystem;

    public TelegramBotService(IConfiguration configuration, TriageSystem triageSystem)
    {
        var botToken = configuration["Telegram:BotToken"]
            ?? throw new InvalidOperationException("Missing Telegram:BotToken in configuration.");

        _botClient = new TelegramBotClient(botToken);
        _triageSystem = triageSystem;
    }

    public void Start()
    {
        using var cts = new CancellationTokenSource();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message }
        };

        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: cts.Token
        );

        Console.WriteLine("Bot is running. Press Enter to stop.");
        Console.ReadLine();
        cts.Cancel();
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message!.Type != MessageType.Text)
            return;

        var chatId = update.Message.Chat.Id;
        var userMessage = update.Message.Text ?? "";

        await foreach (var reply in _triageSystem.RunAsync(chatId.ToString(), userMessage))
        {
            await bot.SendMessage(chatId, reply, cancellationToken: cancellationToken);
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Telegram Error: {exception}");
        return Task.CompletedTask;
    }
}