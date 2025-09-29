#pragma warning disable SKEXP0080

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using sk_multi_agent_system.Plugins;
using static sk_multi_agent_system.Steps.HumanVerifyingStep;

namespace sk_multi_agent_system.Steps;

public class HumanVerifyingStep : KernelProcessStep<HumanVerificationState>
{
    private HumanVerificationState _state = new();

    private string systemPrompt =
    "You are a summarization tool. " +
    "You will be given input when a process is pending termination because a similar bug has already been recorded. " +
    "You will also receive a list of the similar bugs that were found. " +
    "Using this information, create a clear, user-friendly summary that helps the user decide whether to proceed with termination or not.";

    public class HumanVerificationState
    {
        public ChatHistory? ChatHistory { get; set; }
    }

    override public ValueTask ActivateAsync(KernelProcessStepState<HumanVerificationState> state)
    {
        this._state = state.State!;
        this._state.ChatHistory ??= new ChatHistory(systemPrompt);

        return base.ActivateAsync(state);
    }

    [KernelFunction]
    public async Task HumanVerifyAsync(Kernel kernel, string reason, KernelProcessStepContext context)
    {
        Console.WriteLine($"Passed to human verification...");

        IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        // summerizing the content
        this._state.ChatHistory!.AddUserMessage($"Product Info:\n\n{reason}");
        var outputReason = await chatCompletionService.GetChatMessageContentAsync(this._state.ChatHistory!);
        string output = outputReason.Content!.ToString();


    }
}
