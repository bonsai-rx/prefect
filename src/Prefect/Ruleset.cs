using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;

namespace Prefect;

internal sealed class Ruleset
{
    public TemplateKind Kind { get; }
    public ImmutableArray<Rule> Rules { get; }

    public ImmutableArray<Rule>.Enumerator GetEnumerator()
        => Rules.GetEnumerator();

    public Ruleset(string referenceTemplatePath)
    {
        ImmutableArray<Rule>.Builder rules = ImmutableArray.CreateBuilder<Rule>();

        rules.Add(new RepoShouldHaveProjectName());

        //-------------------------------------------------------------------------------------------------------------
        // Determine special template kind and apply blanket rules associated with it
        //-------------------------------------------------------------------------------------------------------------
        // (Note that some rules are applied when files are processed.)
        Kind = TemplateKind.None;
        {
            const string templateKindFileName = ".prefect-template-kind";
            string templateKindPath = Path.Combine(referenceTemplatePath, templateKindFileName);
            if (File.Exists(templateKindPath))
            {
                ReadOnlySpan<char> kindString = File.ReadAllText(templateKindPath).AsSpan().Trim();
                if (Enum.TryParse<TemplateKind>(kindString, out TemplateKind kind))
                    Kind = kind;
                else
                    throw new InvalidOperationException($"Repo template contains '{templateKindFileName}' with unknown template kind '{kindString}'");
            }
        }

        switch (Kind)
        {
            case TemplateKind.BonsaiFoundation:
            {
                rules.Add(new MitLicenseRule());
                rules.Add(new BonsaiFoundationPackageMetadataRule());
                rules.Add(new CSharpProjectsInSrcRule());
                rules.Add(new CSharpProjectValidationRule());
                //rules.Add(new NoBuildStuffOutsideOfBuildFolderRule()); // Too aggressive and probably unecessary
                rules.Add(new BuildInfrastructureOmitsUnecessaryNoiseRule());
                rules.Add(new ModernDotNetGitignoreRule());
                rules.Add(new BonsaiLaunchSettingsJsonRule());
                rules.Add(new ExtraneousLicenseFilesRule());
                rules.Add(new SubmoduleValidationRule());
                rules.Add(new OnlyOneSolutionRule()); // Having more than one complicates keeping CI generic
                rules.Add(new SolutionStructureRule());

                //TODO: Would be nice if these could just go into .prefect-must-not-exist
                rules.Add(new FileGlobDoesNotExistRule(".hgignore"));
                rules.Add(new FileGlobDoesNotExistRule("*.nuspec", (repo, filePath) => repo.ProjectName == "Bonsai.Tld")); // Legacy C++ package
                rules.Add(new FileGlobDoesNotExistRule("NuGet.exe"));
                rules.Add(new FileGlobDoesNotExistRule("NuGet.targets"));
                rules.Add(new FileGlobDoesNotExistRule("NuGet.config", (repo, filePath) => filePath.EndsWith("bonsai/NuGet.config", StringComparison.OrdinalIgnoreCase))); // Matches .bonsai/ and bonsai/
                rules.Add(new FileGlobDoesNotExistRule(".github/workflows/", "*.yml", (repo, filePath) => filePath == $".github/workflows/{repo.ProjectName}.yml"));
                break;
            }
            case TemplateKind.HarpTech:
                //TODO
                break;
            default:
                Debug.Assert(Kind == TemplateKind.None);
                break;
        }

        //-------------------------------------------------------------------------------------------------------------
        // Handle file listings
        //-------------------------------------------------------------------------------------------------------------
        ImmutableHashSet<string> ignoreContentPaths = ImmutableHashSet.CreateRange(EnumerateFileIndex(".prefect-ignore-content"));
        ImmutableHashSet<string> interpolatedFiles = ImmutableHashSet.CreateRange(EnumerateFileIndex(".prefect-interpolated-files"));

        foreach (string path in EnumerateFileIndex(Path.Combine(referenceTemplatePath, ".prefect-must-not-exist")))
            rules.Add(new FileDoesNotExistRule(path));

        IEnumerable<string> EnumerateFileIndex(string fileIndexFileName)
        {
            string fileIndexPath = Path.Combine(referenceTemplatePath, fileIndexFileName);

            if (!File.Exists(fileIndexPath))
                yield break;

            foreach (string line in File.ReadLines(fileIndexPath))
            {
                string path = line.Trim();

                if (path.Length == 0)
                    continue;

                if (path[0] == '#')
                    continue;

                if (path.StartsWith("\\#"))
                    path = path.Substring(1);

                yield return path;
            }
        }

        //-------------------------------------------------------------------------------------------------------------
        // Add rules for files in the template
        //-------------------------------------------------------------------------------------------------------------
        foreach (FileSystemInfo fileSystemInfo in new DirectoryInfo(referenceTemplatePath).EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
        {
            if (fileSystemInfo.Name.StartsWith(".prefect-"))
                continue;

            string relativePath = PathEx.GetNormalRelativeTo(referenceTemplatePath, fileSystemInfo.FullName);

            if (Kind == TemplateKind.BonsaiFoundation)
            {
#if false //TODO: Reintroduce the docfx configuration rule
                if (fileSystemInfo is FileInfo fileInfo && relativePath == DocfxConfigurationRule.RelativePath)
                {
                    rules.Add(new DocfxConfigurationRule(fileInfo));
                    continue;
                }
#endif
            }

            switch (fileSystemInfo)
            {
                case DirectoryInfo:
                    rules.Add(new DirectoryRule(relativePath));
                    break;
                // File with no reference content
                case FileInfo { Length: 0 } fileInfo:
                    rules.Add(new FileExistsRule(relativePath, fileInfo));
                    break;
                // File with reference content
                case FileInfo fileInfo:
                {
                    bool isIgnoreContent = ignoreContentPaths.Contains(relativePath);
                    bool isInterpolated = interpolatedFiles.Contains(relativePath);
                    if (isIgnoreContent && isInterpolated)
                        rules.Add(new InterpolatedFileContentRule(relativePath, fileInfo) { AlwaysOverwrite = false });
                    else if (isIgnoreContent)
                        rules.Add(new FileExistsRule(relativePath, fileInfo));
                    else if (isInterpolated)
                        rules.Add(new InterpolatedFileContentRule(relativePath, fileInfo));
                    else
                        rules.Add(new FileContentRule(relativePath, fileInfo));
                    break;
                }
                default:
                    throw new UnreachableException();
            }
        }

        Rules = rules.ToImmutable();
    }

    public bool ShouldSkip(Rule rule, Repo repo)
    {
        switch (Kind)
        {
            case TemplateKind.BonsaiFoundation:
                // These packages are BSD2 and GPLv3 respectively
                if (repo.RepoSlug is "tld" or "cmt" && rule is MitLicenseRule)
                    return true;
                break;
        }

        return false;
    }
}
