#pragma warning disable SKEXP0110

using Microsoft.Extensions.Configuration;
using sk_multi_agent_system;
using System;
using System.Threading.Tasks;

internal class Program
{
    static async Task Main(string[] args)
    {

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        Console.WriteLine("--- Multi-Agent Triage System Initializing ---");

        var botService = new TelegramBotService(configuration);

        Console.WriteLine("Telegram bot is running.");
        Console.WriteLine("Press Enter to quit.");
        Console.WriteLine("----------------------------------------------------");

        botService.Start();

    }
}