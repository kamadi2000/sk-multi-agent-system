using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("--- Multi-Agent System Initializing ---");

        // --- 1. Instantiate and use the TriageAgent ---
        var triageAgent = new TriageAgent();

        Console.WriteLine($"\n🤖 Executing task on: {triageAgent.Name}...");

        var cts = new CancellationTokenSource();

        await triageAgent.ExecuteAsync(cts.Token);

        // --- 2. Instantiate and use the CodeIntelAgent ---
        var codeIntelAgent = new CodeIntelAgent();

        Console.WriteLine($"\n🤖 Executing task on: {codeIntelAgent.Name}...");

        try
        {
            // Define the task and arguments for the agent
            var historyArgs = new Dictionary<string, object>
            {
                { "partialFileName", "code.ts" } // Change to a file in your repo
            };

            string gitHistory = await codeIntelAgent.ExecuteAsync("FindFileHistory", historyArgs);

            Console.WriteLine("--- Result ---");
            Console.WriteLine(gitHistory);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n--- An error occurred ---");
            Console.WriteLine(ex.Message);
        }


        // --- 3. Instantiate and use the JiraAgent ---
        var jiraAgent = new JiraAgent();

        Console.WriteLine($"\n🤖 Executing task on: {jiraAgent.Name}...");

        try
        {
            // Define the task and arguments for the agent
            var ticketArgs = new Dictionary<string, object>
            {
                { "projectKey", "PROJ" }, // <-- IMPORTANT: Change to your project's key
                { "summary", "Test Bug: Button is not working" },
                { "description", "A bug was reported by the AI agent system." }
            };

            string ticketResult = await jiraAgent.ExecuteAsync("CreateTicket", ticketArgs);

            Console.WriteLine("--- Result ---");
            Console.WriteLine(ticketResult);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n--- An error occurred ---");
            Console.WriteLine(ex.Message);
        }
    }
}