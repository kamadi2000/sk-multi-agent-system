using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("--- Multi-Agent System Initializing ---");

        // initializing the agents
        var codeIntelAgent = new CodeIntelAgent();
        var jiraAgent = new JiraAgent();
        // add comm agent here

        var triageAgent = new TriageAgent(codeIntelAgent, jiraAgent);

        Console.WriteLine($"{triageAgent.Name} is starting....");
        Console.WriteLine("Press Ctrl+C to exit.");

        var cts = new CancellationTokenSource();
        await triageAgent.ExecuteAsync(cts.Token);

    }
}