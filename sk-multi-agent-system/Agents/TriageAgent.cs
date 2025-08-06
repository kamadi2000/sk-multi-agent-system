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
    private string API_KEY = "";
    private string MODEL_ID = "gpt-4.1";
    private string TELEGRAM_BOT_API_KEY = "";
    private Kernel kernel;
    private TelegramBotClient telegramBot;
    private ChatCompletionAgent chatCompletionAgent;
    private readonly Dictionary<long, ChatHistoryAgentThread> userThreads = new();
    private readonly CodeIntelAgent _codeIntelAgent;
    private readonly JiraAgent _jiraAgent;

    public TriageAgent(CodeIntelAgent codeIntelAgent, JiraAgent jiraAgent)
    {
        _codeIntelAgent = codeIntelAgent;
        _jiraAgent = jiraAgent;
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
        // TODO: Update LLM to undestand context and execute
        if (update.Type != UpdateType.Message)
            return;
        if (update.Message!.Type != MessageType.Text)
            return;

        long chatId = update.Message.Chat.Id;
        long userId = update.Message.From.Id;
        string userInput = update.Message.Text!;

        Console.WriteLine($"User {userId} sent: {userInput}");

        await botClient.SendMessage(
            chatId: chatId,
            text: "Understood. Analyzing the file...",
            cancellationToken: cancellationToken);

        var historyArgs = new Dictionary<string, object> { { "partialFileName", userInput } };
        string gitHistoryResult = await _codeIntelAgent.ExecuteAsync("FindFileHistory", historyArgs);

        if (!userThreads.TryGetValue(userId, out var thread))
        {
            thread = new ChatHistoryAgentThread();
            userThreads[userId] = thread;
        }

        string finalPrompt = $"""
        A user is asking for help with a bug. Your task is to analyze the following technical data and present it clearly to them.

        **Technical Data from CodeIntelAgent:**
        ---
        {gitHistoryResult}
        ---

        **Your Instructions:**
        1. Summarize the findings in a user-friendly way.
        2. If the data shows an error (e.g., "File not found"), tell the user you couldn't find the file.
        3. Do not invent any information. Only use the data provided.
        """;

        var result = await HandleThreads(finalPrompt, new Dictionary<string, object>(), thread);

        await botClient.SendMessage(
            chatId: chatId,
            text: result,
            cancellationToken: cancellationToken);

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

