using Atlassian.Jira;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Threading.Tasks;

public class JiraPlugin
{
    // --- Jira Connection Details ---
    // IMPORTANT: Replace with your Jira instance details
    private readonly string _jiraUrl = "https://your-domain.atlassian.net";
    private readonly string _jiraUsername = "your-email@example.com";
    private readonly string _jiraApiToken = "YourJiraApiToken";

    private readonly Jira _jira;

    public JiraPlugin()
    {
        // Creates a connection to your Jira instance
        _jira = Jira.CreateRestClient(_jiraUrl, _jiraUsername, _jiraApiToken);
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
            // Create a new issue object
            var newIssue = _jira.CreateIssue(projectKey);
            newIssue.Type = issueType;
            newIssue.Summary = summary;
            newIssue.Description = description;

            // Save the issue to Jira
            await newIssue.SaveChangesAsync();

            // Return the key of the newly created issue
            return $"Successfully created Jira ticket: {newIssue.Key}";
        }
        catch (Exception ex)
        {
            // Log the full exception for debugging if needed
            Console.WriteLine(ex);
            return $"Error: Failed to create Jira ticket. Check credentials and project key. Details: {ex.Message}";
        }
    }
}