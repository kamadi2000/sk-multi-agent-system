using LibGit2Sharp;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Linq;

public class GitPlugin
{
    
    private readonly string _repoPath = "C:\\Work\\figma-style-export-extention";

    [KernelFunction, Description("Finds a file in the Git repository and gets its first and last commit history.")]
    public string GetGitHistoryForFile(
        [Description("The name of the file to search for, e.g., 'goapplicationmenucontroller.js'")] string fileName
    )
    {
        try
        {
            using (var repo = new Repository(_repoPath))
            {
                // Query the commit log for the specific file
                var commits = repo.Commits.QueryBy(fileName, new CommitFilter { SortBy = CommitSortStrategies.Time })
                                          .ToList();

                if (commits.Count == 0)
                {
                    return $"File '{fileName}' not found or has no history in the repository.";
                }

                // The first commit in the chronological list is the initial commit
                var firstCommit = commits.LastOrDefault();

                // The last commit in the chronological list is the most recent one
                var lastCommit = commits.FirstOrDefault();

                // Format the output string
                var result = $"""
                File History for: {fileName}
                ---------------------------------
                First Commit:
                  Author: {firstCommit?.Commit.Author.Name}
                  Date: {firstCommit?.Commit.Author.When.ToString("yyyy-MM-dd HH:mm:ss")}
                  Message: {firstCommit?.Commit.MessageShort}

                Last Commit:
                  Author: {lastCommit?.Commit.Author.Name}
                  Date: {lastCommit?.Commit.Author.When.ToString("yyyy-MM-dd HH:mm:ss")}
                  Message: {lastCommit?.Commit.MessageShort}
                """;

                return result;
            }
        }
        catch (RepositoryNotFoundException)
        {
            return $"Error: Repository not found at path '{_repoPath}'. Please check the path in GitPlugin.cs.";
        }
        catch (Exception ex)
        {
            return $"An unexpected error occurred: {ex.Message}";
        }
    }

    [KernelFunction, Description("Searches for a file by its partial name in the repository and gets its history.")]
    public string FindFileAndGetHistory(
    [Description("A partial or full name of the file to search for.")] string partialFileName
)
    {
        try
        {
            using (var repo = new Repository(_repoPath))
            {
                string? foundFilePath = null;

                // CORRECTED: Iterate through the Index (a flat list of all files)
                foreach (var entry in repo.Index)
                {
                    // Check if the file path contains the search term (case-insensitive)
                    if (entry.Path.Contains(partialFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        foundFilePath = entry.Path;
                        break; // Stop after finding the first match
                    }
                }

                if (foundFilePath == null)
                {
                    return $"Error: No file found containing the name '{partialFileName}'.";
                }

                // Now that we have the full path, call our existing function to get the history
                return GetGitHistoryForFile(foundFilePath);
            }
        }
        catch (Exception ex)
        {
            return $"An unexpected error occurred: {ex.Message}";
        }
    }
}