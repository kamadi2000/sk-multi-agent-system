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

        var triageSystem = new TriageSystem(configuration);

        Console.WriteLine("System Initialized. You can now chat with the TriageAgent.");
        Console.WriteLine("Type 'exit' to quit.");
        Console.WriteLine("----------------------------------------------------");

        while (true)
        {
            Console.Write("User > ");
            string userInput = Console.ReadLine() ?? "";

            if (string.IsNullOrWhiteSpace(userInput) || userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            await foreach (var message in triageSystem.RunAsync(userInput))
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(message);
                Console.ForegroundColor = originalColor;
            }
            Console.WriteLine();
        }

        Console.WriteLine("--- Session Ended ---");
    }
}