using Microsoft.SemanticKernel;
using System.Collections.Generic;
using System.Threading.Tasks;

public class CodeIntelAgent : IAgent
{
    public string Name => "CodeIntelAgent";
    private readonly Kernel _kernel;

    public CodeIntelAgent()
    {
        var builder = Kernel.CreateBuilder();
        builder.Plugins.AddFromType<GitPlugin>();
        _kernel = builder.Build();
    }

    // Main execution method that routes to the correct task
    public async Task<string> ExecuteAsync(string task, Dictionary<string, object> arguments)
    {
        return task switch
        {
            "FindFileHistory" => await FindFileAndGetHistoryAsync(arguments["partialFileName"].ToString()),
            "ListAllFiles" => await ListAllTrackedFilesAsync(),
            _ => throw new System.NotImplementedException($"Task '{task}' is not supported by {Name}.")
        };
    }

    // --- Private methods for each skill ---

    private async Task<string> FindFileAndGetHistoryAsync(string partialFileName)
    {
        var result = await _kernel.InvokeAsync(
            "GitPlugin",
            "FindFileAndGetHistory",
            new() { ["partialFileName"] = partialFileName }
        );
        return result.GetValue<string>();
    }

    private async Task<string> ListAllTrackedFilesAsync()
    {
        // This would call another function in your GitPlugin that lists all files.
        // For now, we'll just return a placeholder.
        return await Task.FromResult("Listing all tracked files... (implementation pending)");
    }
}