using Microsoft.SemanticKernel;
using System.Collections.Generic;
using System.Threading.Tasks;

public class JiraAgent : IAgent
{
	public string Name => "JiraAgent";
	private readonly Kernel _kernel;

	public JiraAgent()
	{
		var builder = Kernel.CreateBuilder();
		builder.Plugins.AddFromType<JiraPlugin>();
		_kernel = builder.Build();
	}

	public async Task<string> ExecuteAsync(string task, Dictionary<string, object> arguments)
	{
		return task switch
		{
			"CreateTicket" => await CreateTicketAsync(arguments),
			_ => throw new System.NotImplementedException($"Task '{task}' is not supported by {Name}.")
		};
	}

	// --- Private method for the specific skill ---

	private async Task<string> CreateTicketAsync(Dictionary<string, object> arguments)
	{
		var kernelArgs = new KernelArguments
		{
			["projectKey"] = arguments["projectKey"],
			["summary"] = arguments["summary"],
			["description"] = arguments["description"],
			["issueType"] = "Bug"
		};

		var result = await _kernel.InvokeAsync("JiraPlugin", "CreateJiraTicket", kernelArgs);

		return result.GetValue<string>();
	}
}