using Atlassian.Jira;
using Microsoft.SemanticKernel;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace sk_multi_agent_system.Plugins;

public class JiraPlugin
{
    private readonly Jira _jira;

    /// <param name="jiraUrl">Your Jira instance URL.</param>
    /// <param name="jiraUsername">Your Jira username (email).</param>
    /// <param name="jiraApiToken">Your Jira API token.</param>
    public JiraPlugin(string jiraUrl, string jiraUsername, string jiraApiToken)
    {

        _jira = Jira.CreateRestClient(jiraUrl, jiraUsername, jiraApiToken);
    }

    [KernelFunction, Description("Creates a new issue or task in a Jira project.")]
    public async Task<string> CreateJiraTicket(
        [Description("The Jira project key, e.g., 'PROJ'.")] string projectKey,
        [Description("The summary or title for the new issue.")] string summary,
        [Description("The main description for the issue.")] string description,
        [Description("The type of issue to create, e.g., 'Bug', 'Task', 'Story'.")] string issueType = "Task"
    )
    {
        try
        {
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

    [KernelFunction, Description("Assigns an existing Jira issue to a specific user.")]
    public async Task<string> AssignJiraTicket(
        [Description("The key of the issue to assign, e.g., 'PROJ-123'.")] string issueKey,
        [Description("The username or account ID of the person to assign the issue to.")] string assigneeName
    )
    {
        try
        {
            var issue = await _jira.Issues.GetIssueAsync(issueKey);
            if (issue == null)
            {
                return $"Error: Issue with key '{issueKey}' not found.";
            }

            await issue.AssignAsync(assigneeName);

            return $"Successfully assigned issue {issueKey} to {assigneeName}.";
        }
        catch (Exception ex)
        {
            return $"Error: Failed to assign issue {issueKey}. Details: {ex.Message}";
        }
    }
}