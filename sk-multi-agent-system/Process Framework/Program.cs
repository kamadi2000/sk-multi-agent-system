//#pragma warning disable SKEXP0080

//using Microsoft.Extensions.Configuration;
//using Microsoft.SemanticKernel;
//using sk_multi_agent_system.Plugins;
//using sk_multi_agent_system.Processes;

//class Program
//{
//    static async Task Main(string[] args)
//    {
//        var configuration = new ConfigurationBuilder()
//            .AddJsonFile("appSettings.json", optional: true)
//            .AddEnvironmentVariables()
//            .Build();

//        var kernelBuilder = Kernel.CreateBuilder();

//        kernelBuilder.AddOpenAIChatCompletion(
//            configuration["OpenAI:ModelId"],
//            configuration["OpenAI:ApiKey"]
//        );

//        var kernel = kernelBuilder.Build();
//        // Create plugins
//        var gitPlugin = new GitPlugin(configuration, kernel);
//        var jiraPlugin = new JiraPlugin(
//            configuration["Jira:Url"],
//            configuration["Jira:Username"],
//            configuration["Jira:ApiToken"]
//        );

//        // Build the process
//        var process = BugReportProcess.Build(gitPlugin, jiraPlugin);

//        // Start the process with a test bug report
//        var bugReport = "App crashes when clicking Save after editing a profile picture.";
//        //var result = await process.StartAsync(new { Start = bugReport });
//        var result = await process.StartAsync(
//            kernel,
//            new KernelProcessEvent { Id = "Start", Data = bugReport }
//        );

//        Console.WriteLine("Process completed:");
//        Console.WriteLine(result);
//    }
//}
