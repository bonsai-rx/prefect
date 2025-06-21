using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;

namespace Prefect;

internal sealed class ModernDotNetGitignoreRule : FileExistsRule
{
    public override string Description => $"'{RelativePath}' meets the standards for modern .NET projects";

    private static readonly ImmutableSortedSet<string> RequiredPatterns =
    [
        @".vs/",
        @"/artifacts/",
        // Bonsai environments
        @"**/.bonsai/Settings/",
        @"**/.bonsai/Packages/",
        @"**/.bonsai/Bonsai.exe*",
    ];

    private static readonly ImmutableHashSet<string> ForbiddenPatterns =
    [
        @"packages",
        @"Packages",
        @"bin",
        @"bin/",
        @"obj",
        @"obj/",
        @".suo",
        @".vs",
        @"_site",
        @"_site/",
        @".nuget",
        @".nuget/",
        @"Debug",
        @"Release",

        // Overly generic and can hide problems
        @"*.exe",
        @"*.dll",
        @"*.exe.settings",
        @"*.exe.*",

        // Erroneous rules that got spread by mistake
        @".bonsai/Settings/",
        @".bonsai/Packages/",
        @".bonsai/Bonsai.exe*",
    ];

    public ModernDotNetGitignoreRule()
        : base(".gitignore", null)
    { }

    protected override string? Validate(Repo repo, string fullFilePath, string relativeFilePath)
    {
        if (base.Validate(repo, fullFilePath, relativeFilePath) is string failReason)
            return failReason;

        StringBuilder errors = new();

        HashSet<string> missingPatterns = new(RequiredPatterns);
        foreach (string line in File.ReadAllLines(fullFilePath))
        {
            string pattern = line.Trim();
            missingPatterns.Remove(pattern);

            if (ForbiddenPatterns.Contains(pattern))
                errors.Append($"\n    Pattern '{pattern}' is legacy and should be removed.");
        }

        foreach (string pattern in missingPatterns)
            errors.Append($"\n    Pattern '{pattern}' was expected but not found.");

        return errors.Length > 0 ? $"'{relativeFilePath}' does not meet the standard for modern .NET projects:{errors}" : null;
    }

    protected override bool Fixup(Repo repo, string fullFilePath, string relativeFilePath)
    {
        string[] existingPatterns = File.Exists(fullFilePath) ? File.ReadAllLines(fullFilePath) : Array.Empty<string>();
        using StreamWriter sw = new(fullFilePath);

        foreach (string pattern in RequiredPatterns)
            sw.WriteLine(pattern);

        // Guess if the required patterns should be separated from the existing patterns based on whether the file contains multiple sections already
        int blankLineIndex = Array.IndexOf(existingPatterns, "");
        bool wantSeparator = blankLineIndex >= 0 && blankLineIndex < (existingPatterns.Length - 1);

        bool lastWasBlank = false;
        foreach (string line in existingPatterns)
        {
            string trimmedLine = line.Trim();

            // Skip lines already written or not allowed
            if (RequiredPatterns.Contains(trimmedLine) || ForbiddenPatterns.Contains(trimmedLine))
                continue;

            // Write out implicit separator if needed
            if (wantSeparator)
            {
                sw.WriteLine();
                lastWasBlank = true;
                wantSeparator = false;
            }

            // Skip redundant blank lines
            if (lastWasBlank && trimmedLine.Length == 0)
                continue;

            // Write out the existing pattern
            sw.WriteLine(line);
            lastWasBlank = trimmedLine.Length == 0;
        }

        return true;
    }
}
