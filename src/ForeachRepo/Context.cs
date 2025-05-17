using System;
using System.Collections.Generic;

namespace ForeachRepo;

internal record class Context()
{
    public required OutputMode OutputMode { get; init; }
    public required string RepoPath { get; init; }
    public required string RepoSlug { get; init; }

    public string GetVariable(ReadOnlySpan<char> variable)
    {
        switch (variable)
        {
            case nameof(RepoPath):
            case "REPO_PATH":
                return RepoPath;
            case nameof(RepoSlug):
            case "REPO_SLUG":
                return RepoSlug;
            default:
                throw new ArgumentException($"Unknown variable '{variable}'", nameof(variable));
        }
    }

    public static IEnumerable<(string name, string description)> EnumerateVariables()
    {
        yield return ($"{nameof(RepoPath)}/REPO_PATH", "");
        yield return ($"{nameof(RepoPath)}/REPO_SLUG", "");
    }
}
