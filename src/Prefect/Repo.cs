using System;
using System.Collections.Generic;
using System.IO;

namespace Prefect;

internal sealed class Repo
{
    public string RootPath { get; }
    public string ProjectName { get; }
    public string RepoSlug { get; }
    public bool HasValidProjectName { get; }

    public Repo(string rootPath, TemplateKind kind)
    {
        RootPath = rootPath;
        RepoSlug = Path.GetFileName(Path.TrimEndingDirectorySeparator(RootPath));

        string? projectName = null;
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

    public string GetRelativePath(string referencePath)
        => referencePath.Replace("$PROJECT$", ProjectName);

    public string GetFullPath(string relativeReferencePath, out string relativePath)
        => Path.Combine(RootPath, relativePath = GetRelativePath(relativeReferencePath));

    public string GetFullPath(string relativeReferencePath)
        => GetFullPath(relativeReferencePath, out _);

    private IEnumerable<string> EnumerateFilesImpl(string basePath, string searchPattern, SearchOption searchOption = SearchOption.AllDirectories)
    {
        if (!Directory.Exists(basePath))
            yield break;

        foreach (string filePath in Directory.EnumerateFiles(basePath, searchPattern, searchOption))
        {
            string relativePath = PathEx.GetNormalRelativeTo(RootPath, filePath);

            //TODO: Actual .gitignore support would be nice
            if (relativePath.StartsWith("artifacts/") || relativePath.StartsWith(".bonsai/Packages/"))
                continue;

            yield return filePath;
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
}
