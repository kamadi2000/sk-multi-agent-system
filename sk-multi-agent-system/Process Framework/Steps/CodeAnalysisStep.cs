#pragma warning disable SKEXP0080

using Microsoft.SemanticKernel;
using sk_multi_agent_system.Plugins;

namespace sk_multi_agent_system.Steps;

public class CodeAnalysisStep : KernelProcessStep
{

    [KernelFunction]
    public async Task<string> AnalyzeBugAsync(KernelProcessStepContext context)
    {
        //Console.WriteLine($"[{nameof(CodeAnalysisStep)}]: Analyzing bug...");
        //var results = await _gitPlugin.SemanticSearchCommits(bugReport);

        //await context.EmitEventAsync("BugAnalyzed", results);
        //return results;
        var results = "Anlysing the issue";
        Console.WriteLine($"[{nameof(CodeAnalysisStep)}]: Anlysing the issue");
        
        await context.EmitEventAsync("BugAnalyzed", results);
        return results;
    }
}
