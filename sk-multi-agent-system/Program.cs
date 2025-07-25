using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

string API_KEY = "";
string MODEL_ID = "gpt-4.1";

// Create a Kernel
var builder = Kernel.CreateBuilder();
builder.AddOpenAIChatCompletion(MODEL_ID, API_KEY);
var kernel = builder.Build();

// Get the chat service
var chatService = kernel.GetRequiredService<IChatCompletionService>();

// Create a chat history
ChatHistory chatMessages = new ChatHistory();
chatMessages.AddUserMessage("What's the capital of France?");

// Send the chat to the LLM and get a response
var reply = await chatService.GetChatMessageContentAsync(chatMessages);

// Print the result
Console.WriteLine($"Assistant: {reply.Content}");