#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0050

using LibGit2Sharp;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.MongoDB;
using MongoDB.Driver;
using sk_multi_agent_system;
using sk_multi_agent_system.Models;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        try
        {
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            // This will catch *startup* exceptions
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FATAL] Application failed to start: {ex}");
            Console.ResetColor();
            throw; // rethrow so the host still knows it crashed
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                var env = hostingContext.HostingEnvironment;

                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((hostContext, services) =>
            {
                var configuration = hostContext.Configuration;

                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                });

                // Register your services
                services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(configuration["Telegram:BotToken"]!));

                services.AddSingleton<IUserChatService, UserChatService>();
                services.AddSingleton<TriageSystem>();
                services.AddSingleton<TelegramBotService>();
                services.AddHostedService<TelegramBotWorker>();
            });
}
