#pragma warning disable SKEXP0110

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using sk_multi_agent_system;
using System.Threading.Tasks;

internal class Program
{
    public static async Task Main(string[] args)
    {
        await CreateHostBuilder(args).Build().RunAsync();
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
                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                });

                // Register your services
                services.AddSingleton<TelegramBotService>();
                services.AddHostedService<TelegramBotWorker>();
            });
}
