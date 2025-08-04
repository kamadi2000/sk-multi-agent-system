using Azure;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

public class TriageAgent : IAgent
{
    public string Name => "TriageAgent";
    private string API_KEY = "API_KEY";
    private string MODEL_ID = "gpt-4.1";
    private string TELEGRAM_BOT_API_KEY = "TELEGRAM_APIKEY";
    private Kernel kernel;
    private TelegramBotClient telegramBot;
    private ChatCompletionAgent chatCompletionAgent;
    private readonly Dictionary<long, ChatHistoryAgentThread> userThreads = new();

    public TriageAgent()
    {
        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(MODEL_ID, API_KEY);
        kernel = builder.Build();

        chatCompletionAgent = new ChatCompletionAgent
        {
            Name = Name,
            Instructions = "You are a helpful assistant who replies concisely.",
            Kernel = kernel,
            Arguments = new KernelArguments(new OpenAIPromptExecutionSettings()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            })
            {
                { "repository", "microsoft/semantic-kernel" }
            }
        };
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        telegramBot = new TelegramBotClient(TELEGRAM_BOT_API_KEY);

        ReceiverOptions receiverOptions = new()
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        telegramBot.StartReceiving(
            async (botClient, update, token) =>
                await HandleUpdateAsync(botClient, update, token),
            HandleErrorAsync,
            receiverOptions,
            cancellationToken
        );

        Console.WriteLine("Bot is running. Press Ctrl+C to stop.");
        await Task.Delay(-1, cancellationToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message)
            return;
        if (update.Message!.Type != MessageType.Text)
            return;

        long chatId = update.Message.Chat.Id;
        long userId = update.Message.From.Id;
        string userInput = update.Message.Text!;

        Console.WriteLine($"User {userId} sent: {userInput}");

        if (!userThreads.TryGetValue(userId, out var thread))
        {
            thread = new ChatHistoryAgentThread();
            userThreads[userId] = thread;
        }

        var result = await HandleThreads(update.Message?.Text, new Dictionary<string, object>
        {
            { "now", DateTime.Now.ToString() }
        }, thread);

        await botClient.SendMessage(chatId: chatId, text: result);
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        // Error message based on exception type
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        // Print error message to console
        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }

    public Task<string> ExecuteAsync(string task, Dictionary<string, object> arguments)
    {
        throw new NotImplementedException();
    }

    // Overload to use a specific agent thread
    private async Task<string> HandleThreads(string task, Dictionary<string, object> arguments, ChatHistoryAgentThread thread)
    {
        var message = new ChatMessageContent(AuthorRole.User, task);
        var kernelArgs = new KernelArguments();

        foreach (var kvp in arguments)
        {
            kernelArgs[kvp.Key] = kvp.Value;
        }

        string responseText = "";

        await foreach (ChatMessageContent response in chatCompletionAgent.InvokeAsync(message, thread, options: new() { KernelArguments = kernelArgs }))
        {
            responseText += response.Content;
        }

        return responseText;
    }
}

