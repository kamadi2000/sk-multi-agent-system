#pragma warning disable SKEXP0080

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static sk_multi_agent_system.Steps.TerminationStep;

namespace sk_multi_agent_system.Steps;

public class TerminationStep: KernelProcessStep<TerminationState>
{ 
    private TerminationState _state = new();

    private string systemPrompt =
    "You are a text formatting tool. " +
    "You will receive an input when a process is terminated because a similar bug was already recorded earlier. " +
    "You will also receive the list of similar bugs that were found. " +
    "Using this information, generate a clear and user-friendly explanation for the termination.";

    public class TerminationState
    {
        public ChatHistory? ChatHistory { get; set; }
    }

    override public ValueTask ActivateAsync(KernelProcessStepState<TerminationState> state)
    {
        this._state = state.State!;
        this._state.ChatHistory ??= new ChatHistory(systemPrompt);

        return base.ActivateAsync(state);
    }

    [KernelFunction]
    public async Task TerminateProcess(Kernel kernel, string reason, KernelProcessStepContext context)
    {
        Console.WriteLine("Terminating process...");
        IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        // adding reason to terminate
        this._state.ChatHistory!.AddUserMessage($"Product Info:\n\n{reason}");
        var outputReason = await chatCompletionService.GetChatMessageContentAsync(this._state.ChatHistory!);
        string output = outputReason.Content!.ToString();

        Console.WriteLine($"Process terminated: {output}");
    }
}

