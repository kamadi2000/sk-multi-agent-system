using LibGit2Sharp;
using Microsoft.SemanticKernel;
using System;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace sk_multi_agent_system.Plugins;

public class GitPlugin
{
    // TODO: update instead of hardcoded.
    private readonly string _repoPath = "C:\\Work\\sk-multi-agent-system";

    [KernelFunction, Description("Finds a file by its partial or full name and gets its first and last commit history.")]
    public string GetFileCommitHistory(
        [Description("The partial or full name of the file to search for.")] string fileName
    )
    {
        try
        {
            using var repo = new Repository(_repoPath);

            // find the full path of the file from a partial name
            string? foundFilePath = repo.Index
                .Select(entry => entry.Path)
                .FirstOrDefault(path => path.Contains(fileName, StringComparison.OrdinalIgnoreCase));

            if (foundFilePath == null)
            {
                return $"Error: No file found containing the name '{fileName}'.";
            }

            // get the commit history for the full file path
            var commits = repo.Commits.QueryBy(foundFilePath, new CommitFilter { SortBy = CommitSortStrategies.Time }).ToList();
            if (!commits.Any())
            {
                return $"File '{foundFilePath}' found, but it has no commit history.";
            }

            var firstCommit = commits.Last().Commit;
            var lastCommit = commits.First().Commit; 

            // Use StringBuilder for structured output
            var output = new StringBuilder();
            output.Append($"File: {foundFilePath}|");
            output.Append($"FirstCommitAuthor: {firstCommit.Author.Name}|");
            output.Append($"FirstCommitDate: {firstCommit.Author.When:yyyy-MM-dd}|");
            output.Append($"FirstCommitMessage: {firstCommit.MessageShort}|");
            output.Append($"LastCommitAuthor: {lastCommit.Author.Name}|");
            output.Append($"LastCommitDate: {lastCommit.Author.When:yyyy-MM-dd}|");
            output.Append($"LastCommitMessage: {lastCommit.MessageShort}");

            return output.ToString();
        }
        catch (Exception ex)
        {
            return $"An unexpected error occurred: {ex.Message}";
        }
    }

    [KernelFunction, Description("Gets a list of the most recent commits for the entire repository.")]
    public string GetRecentRepositoryCommits(
        [Description("The number of recent commits to retrieve.")] int limit = 5
    )
    {
        try
        {
            using var repo = new Repository(_repoPath);
            var commits = repo.Commits.Take(limit).ToList();

            if (!commits.Any())
            {
                return "No commits found in the repository.";
            }

            // Use StringBuilder to format the list of commits
            var output = new StringBuilder();
            output.AppendLine("Recent Commits:");
            foreach (var commit in commits)
            {
                output.Append($"Commit: {commit.Id.ToString(7)}|");
                output.Append($"Author: {commit.Author.Name}|");
                output.Append($"Date: {commit.Author.When:yyyy-MM-dd}|");
                output.Append($"Message: {commit.MessageShort}");
                output.AppendLine();
            }

            return output.ToString();
        }
        catch (Exception ex)
        {
            return $"An unexpected error occurred: {ex.Message}";
        }
    }

    [KernelFunction, Description("Gets a list of files that were changed (added, modified, deleted) in a specific commit.")]
    public string GetCommitChanges(
    [Description("The 7-character commit SHA to inspect (e.g., 'a1b2c3d').")] string commitSha
)
    {
        try
        {
            using var repo = new Repository(_repoPath);

            // Find the commit by its SHA
            var commit = repo.Lookup<Commit>(commitSha);
            if (commit == null)
            {
                return $"Error: Commit with SHA '{commitSha}' not found.";
            }

            // The first commit has no parents to compare against
            if (!commit.Parents.Any())
            {
                return $"Commit '{commitSha}' is the initial commit. All files were added.";
            }

            var parentCommit = commit.Parents.First();
            var changes = repo.Diff.Compare<TreeChanges>(parentCommit.Tree, commit.Tree);

            var output = new StringBuilder();
            output.AppendLine($"Changes in commit {commit.Id.ToString(7)}:");

            foreach (var entry in changes.Added)
            {
                output.AppendLine($"  - Added: {entry.Path}");
            }
            foreach (var entry in changes.Modified)
            {
                output.AppendLine($"  - Modified: {entry.Path}");
            }
            foreach (var entry in changes.Deleted)
            {
                output.AppendLine($"  - Deleted: {entry.Path}");
            }

            if (output.Length == 0)
            {
                return $"No file changes detected in commit {commit.Id.ToString(7)} (e.g., merge commit with no changes).";
            }

            return output.ToString();
        }
        catch (Exception ex)
        {
            return $"An unexpected error occurred: {ex.Message}";
        }
    }
}