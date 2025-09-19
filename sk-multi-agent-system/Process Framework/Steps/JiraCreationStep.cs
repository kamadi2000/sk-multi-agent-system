#pragma warning disable SKEXP0080

using Atlassian.Jira;
using Microsoft.SemanticKernel;
using sk_multi_agent_system.Plugins;
using System.ComponentModel;

namespace sk_multi_agent_system.Steps;

public class JiraCreationStep : KernelProcessStep
{
    //private readonly JiraPlugin _jiraPlugin;

    //public JiraCreationStep(JiraPlugin jiraPlugin)
    //{
    //	_jiraPlugin = jiraPlugin;
    //}
    private readonly Jira _jira;

    [KernelFunction]
	public async Task<string> CreateTicketAsync(string analyzedInfo, KernelProcessStepContext context)
	{
		Console.WriteLine($"[{nameof(JiraCreationStep)}]: Jira ticket creating...");
		var projectKey = "AG";
		var summary = "Bug Report from Analysis";
		var description = analyzedInfo;
		var issueType = "Bug";

        //var result = await _jiraPlugin.CreateJiraTicket(
        //	projectKey,
        //	summary,
        //	description,
        //	issueType
        //);
        var result = $"Ticket Created! [Project: {projectKey}, Summary: {summary}, Description: {description}, Type: {issueType}]";

        var ticket = await CreateJiraTicket(projectKey, summary, description, issueType);

        await context.EmitEventAsync("TicketCreated", ticket);
		return ticket;
	}



    private async Task<string> CreateJiraTicket(
        [Description("The Jira project key, e.g., 'PROJ'.")] string projectKey,
        [Description("The summary or title for the new issue.")] string summary,
        [Description("The main description for the issue.")] string description,
        [Description("The type of issue to create, e.g., 'Bug', 'Task', 'Story'.")] string issueType = "Task"
    )
    {
        try
        {
            var jiraUrl = "";
            var jiraUsername = "";
            var jiraApiToken = "";
            var _jira = Jira.CreateRestClient(jiraUrl, jiraUsername, jiraApiToken);
            var newIssue = _jira.CreateIssue(projectKey);
            newIssue.Type = issueType;
            newIssue.Summary = summary;
            newIssue.Description = description;

            await newIssue.SaveChangesAsync();

            return $"Successfully created Jira ticket: {newIssue.Key}";
        }
        catch (Exception ex)
        {
            return $"Error: Failed to create Jira ticket. Details: {ex.Message}";
        }
    }
}
