using Microsoft.VisualStudio.SolutionPersistence;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Prefect;
internal sealed class SolutionStructureRule : Rule
{
    public override string Description => "Solution reflects the contents of the repository.";

    public override string? Validate(Repo repo)
        => ValidateOrFix(repo, applyFix: false).Result.errors;

    public override bool Fixup(Repo repo)
        => ValidateOrFix(repo, applyFix: true).Result.result == Result.FixesApplied;

    private static readonly Guid LegacyCSharpProjectType = new("9A19103F-16F7-4668-BE54-9A1E7A4F7556");
    private static readonly Guid ModernCSharpProjectType = new("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC");

    private async Task<(Result result, string? errors)> ValidateOrFix(Repo repo, bool applyFix)
    {
        if (!repo.HasValidProjectName)
            return (Result.ValidationFailed, "Repo must have solution in root that we can identify as primary.");

        string relativeSolutionPath = $"{repo.ProjectName}.sln";
        string solutionPath = Path.Combine(repo.RootPath, relativeSolutionPath);
        if (!File.Exists(solutionPath))
            return (Result.ValidationFailed, $"Could not open solution '{relativeSolutionPath}'");

        ISolutionSerializer? serializer = SolutionSerializers.GetSerializerByMoniker(solutionPath);
        if (serializer is null)
            return (Result.ValidationFailed, $"Could not initialize solution seiralizer for '{relativeSolutionPath}'");

        SolutionModel solution = await serializer.OpenAsync(solutionPath, CancellationToken.None);
        StringBuilder _errors = new();
        bool canFixAutomatically = true;

        void Error(string message, bool fatal = false)
        {
            if (_errors.Length == 0)
                _errors.AppendLine($"{solutionPath} has problems:");

            _errors.AppendLine($"  {message}");

            if (fatal)
                canFixAutomatically = false;
        }

        //===========================================================================================================================================
        // Handle projects
        //===========================================================================================================================================
        // Determine what projects should be present
        Dictionary<string, string> expectedProjects = new();
        foreach (string projectPath in repo.EnumerateFiles("*.csproj", SearchOption.AllDirectories).Concat(repo.EnumerateFiles("*.vcxproj", SearchOption.AllDirectories)))
        {
            string projectFileName = Path.GetFileName(projectPath);
            if (!expectedProjects.TryAdd(projectFileName, Path.GetRelativePath(repo.RootPath, projectPath)))
                Error($"Repo contains more than one project named '{projectFileName}'", fatal: true);
        }

        // Scan/fix projects
        List<SolutionProjectModel> projectsToRemove = new();
        foreach (SolutionProjectModel project in solution.SolutionProjects)
        {
            string projectFileName = Path.GetFileName(project.FilePath);
            if (!expectedProjects.Remove(projectFileName, out string? expectedProject))
            {
                Error($"Project '{project.FilePath}' points to a non-existent project!");
                projectsToRemove.Add(project);
                continue;
            }

            if (project.FilePath != expectedProject)
            {
                Error($"Project '{project.FilePath}' exists but points to an outdated location.");
                project.FilePath = expectedProject;
            }

            if (project.Parent is not null)
            {
                Error($"Project '{project.FilePath}' is nested under a solution folder.");
                project.MoveToFolder(null);
            }

            if (project.TypeId == LegacyCSharpProjectType)
            {
                Error($"Project '{project.FilePath}' is using a legacy C# project GUID");
                project.Type = ModernCSharpProjectType.ToString();
            }
        }

        // Remove non-existent projects
        foreach (SolutionProjectModel project in projectsToRemove)
            solution.RemoveProject(project);

        // Add missing projects
        foreach ((string projectFileName, string projectPath) in expectedProjects)
        {
            Error($"'{projectPath}' is not present.");
            solution.AddProject(projectPath);
        }

        //===========================================================================================================================================
        // Solution folders (and items)
        //===========================================================================================================================================
        // Determine what files should be present
        Dictionary<string, List<string>> expectedFolders = new();
        foreach (string itemPath in repo.EnumerateFiles("build", "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(repo.RootPath, itemPath);
            string relativeDirectoryPath = Path.GetDirectoryName(relativePath) ?? "";

            // Only the folder path uses forward slashes!
            // (It's not something actually present in the solution file, it's inferred.)
            relativeDirectoryPath = PathEx.NormalSlashes(relativeDirectoryPath);

            string folderKey = $"/{relativeDirectoryPath}/";
            if (!expectedFolders.TryGetValue(folderKey, out List<string>? files))
                expectedFolders.Add(folderKey, files = new());

            files.Add(relativePath);
        }

        // Scan/fix solution folders (and items)
        List<SolutionFolderModel> foldersToRemove = new();
        foreach (SolutionFolderModel folder in solution.SolutionFolders)
        {
            if (!expectedFolders.Remove(folder.Path, out List<string>? files))
            {
                Error($"Solution folder '{folder.Path}' should not exist");
                foldersToRemove.Add(folder);
                continue;
            }

            // Remove all files
            HashSet<string> actuallyRemoved = new();
            while (folder.Files?.Count > 0)
            {
                string file = folder.Files[0];
                folder.RemoveFile(file);
                actuallyRemoved.Add(file);
            }

            // Re-add expected files
            foreach (string file in files)
            {
                if (!actuallyRemoved.Remove(file))
                    Error($"File '{file}' is missing.");
                folder.AddFile(file);
            }

            // Note all files that should not have been there
            foreach (string file in actuallyRemoved)
                Error($"File '{file}' should not be present.");
        }

        // Remove unexpected folders
        foreach (SolutionFolderModel folder in foldersToRemove)
            solution.RemoveFolder(folder);

        // Add missing folders
        foreach ((string directoryPath, List<string> files) in expectedFolders.OrderBy(p => p.Key))
        {
            SolutionFolderModel folder = solution.AddFolder(directoryPath);
            Error($"'{directoryPath}' is missing.");

            foreach (string file in files)
                folder.AddFile(file);
        }

        //===========================================================================================================================================
        // Validate configurations/platforms
        //===========================================================================================================================================
        if (solution.RemovePlatform("Mixed Platforms"))
            Error("'Mixed Platforms' platform is not permitted.");

        // Distilling project configurations is nice, but it doesn't tell you if it did anything so we need to observe side-effects so we have to compare before and after
        // (Or at least in theory...for some reason the configuration mapping is just missing after distilling?)
#if false
        List<(SolutionProjectModel project, ConfigurationRule[] rules)> oldRules = new();
        foreach (SolutionProjectModel project in solution.SolutionProjects)
            oldRules.Add((project, project.ProjectConfigurationRules is null ? [] : [.. project.ProjectConfigurationRules]));

        solution.DistillProjectConfigurations();

        foreach ((SolutionProjectModel project, ConfigurationRule[] rules) in oldRules)
        {
            void BadConfig()
                => Error($"'{project.FilePath}' has improper project configuration rules.");

            IReadOnlyList<ConfigurationRule>? newRules = project.ProjectConfigurationRules;

            if (newRules is null)
            {
                if (rules.Length > 0)
                    BadConfig();
                continue;
            }

            if (rules.Length != newRules.Count)
            {
                BadConfig();
                continue;
            }

            for (int i = 0; i < rules.Length; i++)
            {
                ConfigurationRule a = rules[i];
                ConfigurationRule b = project.ProjectConfigurationRules![i];

                if (a.Dimension != b.Dimension
                    || a.ProjectValue != b.ProjectValue
                    || a.SolutionBuildType != b.SolutionBuildType
                    || a.SolutionPlatform != b.SolutionPlatform)
                {
                    BadConfig();
                    break;
                }
            }
        }
#endif

        //===========================================================================================================================================
        // Determine verdict
        //===========================================================================================================================================
        if (_errors.Length == 0)
            return (Result.ValidationPassed, null);

        if (applyFix && canFixAutomatically)
        {
            await serializer.SaveAsync(solutionPath, solution, CancellationToken.None);
            return (Result.FixesApplied, _errors.ToString());
        }

        return (Result.ValidationFailed, _errors.ToString());
    }

    private enum Result
    {
        ValidationPassed,
        ValidationFailed,
        FixesApplied,
    }
}
