using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Text.RegularExpressions;
using GitRepository = LibGit2Sharp.Repository;

namespace Prefect;

internal sealed partial class Repo
{
    public string RootPath { get; }
    public string RepoSlug { get; }

    public GitRepository Git { get; }

    public string ProjectName { get; }
    public bool HasValidProjectName { get; }

    public Repo(string rootPath, TemplateKind kind, string? projectName)
    {
        RootPath = rootPath;
        RepoSlug = Path.GetFileName(Path.TrimEndingDirectorySeparator(RootPath));

        Git = new GitRepository(rootPath);
        if (Git.Info.IsBare)
            throw new NotSupportedException("Bare Git repositories are not supported.");

        string mainSolutionFolder = kind != TemplateKind.HarpTech ? "" : "Interface";
        foreach (string solutionFile in EnumerateFiles(mainSolutionFolder, "*.sln", SearchOption.TopDirectoryOnly))
        {
            string solutionName = Path.GetFileNameWithoutExtension(solutionFile);
            if (projectName is null || projectName.Length > solutionName.Length)
                projectName = solutionName;
        }

        ProjectName = projectName ?? "UNKNOWN";
        HasValidProjectName = projectName is not null;
    }

    [GeneratedRegex(@"\$(?<variable>[A-Za-z0-9_\-]+)\$")]
    private static partial Regex VariableRegex();

    public string? EvaluateInterpolationHole(ReadOnlySpan<char> variableName)
        => variableName switch
        {
            "PROJECT" => ProjectName,
            "REPO-SLUG" => RepoSlug,
            _ => null,
        };

    public delegate string UnknownVariableFallback(ReadOnlySpan<char> variable);
    public string EvaluateTemplate(string template, UnknownVariableFallback unknownVariableFallback)
        => VariableRegex().Replace
        (
            template,
            (match) => match.Groups["variable"].ValueSpan switch
            {
                "PROJECT" => ProjectName,
                "REPO-SLUG" => RepoSlug,
                var unknownVariable => unknownVariableFallback(unknownVariable),
            }
        );

    public string GetRelativePath(string referencePath)
        => EvaluateTemplate(referencePath, variable => variable.ToString());

    public string GetFullPath(string relativeReferencePath, out string relativePath)
        => Path.Combine(RootPath, relativePath = GetRelativePath(relativeReferencePath));

    public string GetFullPath(string relativeReferencePath)
        => GetFullPath(relativeReferencePath, out _);

    private IEnumerable<string> EnumerateFilesImpl(string basePath, string searchPattern, SearchOption searchOption = SearchOption.AllDirectories)
    {
        if (!Directory.Exists(basePath))
            yield break;

        Queue<DirectoryInfo> directories = new();
        directories.Enqueue(new DirectoryInfo(basePath));

        while (directories.Count > 0)
        {
            DirectoryInfo directory = directories.Dequeue();
            foreach (FileSystemInfo fileSystemInfo in directory.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly))
            {
                string relativePath = PathEx.GetNormalRelativeTo(RootPath, fileSystemInfo.FullName);

                // Skip files ignored by Git
                if (Git.Ignore.IsPathIgnored(relativePath))
                    continue;

                switch (fileSystemInfo)
                {
                    // Recurse if requested
                    case DirectoryInfo childDirectory when searchOption == SearchOption.AllDirectories:
                        directories.Enqueue(childDirectory);
                        break;
                    // Yield files matching the pattern
                    case FileInfo fileInfo when FileSystemName.MatchesSimpleExpression(searchPattern, fileInfo.Name):
                        yield return fileInfo.FullName;
                        break;
                }
            }
        }
    }

    public IEnumerable<string> EnumerateFiles(string subdirectory, string searchPattern, SearchOption searchOption = SearchOption.AllDirectories)
    {
        if (Path.IsPathRooted(subdirectory))
            throw new ArgumentException($"'{subdirectory}' does not represent a relative subdirectory path", nameof(subdirectory));

        return EnumerateFilesImpl(Path.Combine(RootPath, subdirectory), searchPattern, searchOption);
    }

    public IEnumerable<string> EnumerateFiles(string searchPattern, SearchOption searchOption = SearchOption.AllDirectories)
        => EnumerateFilesImpl(RootPath, searchPattern, searchOption);

    public IEnumerable<string> EnumerateFiles(SearchOption searchOption = SearchOption.AllDirectories)
        => EnumerateFiles("*", searchOption);

    /// <summary>Checks if the specified path is a Git repository</summary>
    /// <remarks>
    /// It's very intentional that Prefect only tries to validate directories identifiable as Git repositories.
    /// Running Prefect is generally a destructive operation, and we don't want people using it in a context
    /// where they might be bothered by things getting overwritten or deleted, especially by mistake.
    ///
    /// For example, if somene accidentally ran something like `prefect my-template / --auto-fix`, we would
    /// definitely not want it to recurse though the entire system deleting all the .hgignore files it finds
    /// because there's a rule deeming them legacy.
    ///
    /// Note that unlike Git, we don't want to recurse up. The path must be the root of the Git repo (which may be a submodule.)
    /// </remarks>
    public static bool IsRepository(string path)
        => GitRepository.IsValid(path);
}
