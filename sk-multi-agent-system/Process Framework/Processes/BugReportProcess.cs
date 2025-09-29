#pragma warning disable SKEXP0080

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Process;
using sk_multi_agent_system.Plugins;
using sk_multi_agent_system.Steps;
using Microsoft.Extensions.AI;


namespace sk_multi_agent_system.Processes;

public static class BugReportProcess
{
    public static KernelProcess Build()
    {
        var builder = new ProcessBuilder("BugReportProcess");

        // Add steps
        var intakeStep = builder.AddStepFromType<BugIntakeStep>();
        var analysisStep = builder.AddStepFromType<CodeAnalysisStep>();
        var jiraStep = builder.AddStepFromType<JiraCreationStep>();
        var terminationStep = builder.AddStepFromType<TerminationStep>();
        var humanVerifyingStep = builder.AddStepFromType<HumanVerifyingStep>();

        // Wire events
        builder.OnInputEvent("Start").SendEventTo(new(intakeStep));
        intakeStep.OnEvent("BugReceived").SendEventTo(new(analysisStep));
        intakeStep.OnEvent("DuplicateFound").SendEventTo(new(terminationStep));
        intakeStep.OnEvent("HumanVerificationNeeded").SendEventTo(new(humanVerifyingStep));
        analysisStep.OnEvent("BugAnalyzed").SendEventTo(new(jiraStep));
        
        return builder.Build();
    }
}
