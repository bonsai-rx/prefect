using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Prefect;

internal sealed class SubmoduleValidationRule : Rule
{
    public override string Description => $"Verify submodule usage";

    private struct Submodule
    {
        public string Name;
        public string Path; //TODO: P sure this is optional
        public string Url;
    }

    public override string? Validate(Repo repo)
    {
        string relativePath = ".gitmodules";
        string gitmodulesPath = Path.Combine(repo.RootPath, relativePath);
        if (!File.Exists(gitmodulesPath))
            return null;

        StringBuilder errors = new();

        // Read in submodules
        // Probably not the most robust parsing, but it'll get the job done
        // (This file is almost always manipulated via Git and not directly, so it should be consistent.)
        List<Submodule> submodules = new();
        {
            int lineNumber = 0;
            string? name = null;
            string? path = null;
            string? url = null;
            foreach (string _line in File.ReadAllLines(gitmodulesPath).Concat([""])) // Concat assures we always run an extra loop to take care of the final section
            {
                lineNumber++;
                ReadOnlySpan<char> line = _line.AsSpan().Trim();

                // Check if we finished a section on the previous line
                if (name is not null && path is not null && url is not null)
                {
                    submodules.Add(new Submodule()
                    {
                        Name = name,
                        Path = path,
                        Url = url,
                    });
                    name = path = url = null;
                }


                // New section
                const string sectionStartPrefix = "[submodule \"";
                const string sectionEndSuffix = "\"]";
                if (line.StartsWith(sectionStartPrefix) && line.EndsWith(sectionEndSuffix))
                {
                    if (name is not null || path is not null || url is not null)
                        errors.AppendLine($"{relativePath}:{lineNumber} starts a new submodule but we didn't finish parsing the last one!");
                    name = line.Slice(sectionStartPrefix.Length, line.Length - sectionStartPrefix.Length - sectionEndSuffix.Length).ToString();
                    path = url = null;
                    continue;
                }

                // Path parameter
                const string pathPrefix = "path = ";
                if (line.StartsWith(pathPrefix))
                {
                    path = line.Slice(pathPrefix.Length).ToString();
                    continue;
                }

                // URL parameter
                const string urlPrefix = "url = ";
                if (line.StartsWith(urlPrefix))
                {
                    line = line.Slice(urlPrefix.Length).ToString();

                    // Don't allow SSH or off-platform submodules
                    if (!line.StartsWith("https://github.com/"))
                        errors.AppendLine($"{relativePath}:{lineNumber} '{line}' is not an HTTPS GitHub URL");

                    const string gitSuffix = ".git";
                    if (line.EndsWith(gitSuffix))
                        line = line.Slice(0, line.Length - gitSuffix.Length);
                    if (line.EndsWith("/"))
                        line = line.Slice(0, line.Length - 1);

                    url = line.ToString().ToLowerInvariant();
                    continue;
                }

                // Skip blanks
                if (line.Length == 0)
                    continue;

                // Ignore the branch setting
                if (line.StartsWith("branch = "))
                    continue;

                // If we got this far we don't know what to do with this line
                // There are other options that can appear in the .gitmodules file, but they aren't common so we don't tolerate them for now
                errors.AppendLine($"Not sure how to parse '{line}' from '{relativePath}' @ line {lineNumber}");
            }

            if (name is not null || path is not null || url is not null)
                errors.AppendLine($"Finished reading submodules file but we never finished parsing the last entry!");
        }

        foreach (Submodule submodule in submodules)
        {
            string relativeGitFolder = Path.Combine(".git", "modules", submodule.Name);
            string gitFolder = Path.Combine(repo.RootPath, relativeGitFolder);
            string headFilePath = Path.Combine(gitFolder, "HEAD");
            string submoduleGitPointerPath = Path.Combine(repo.RootPath, submodule.Path, ".git");

            if (!File.Exists(headFilePath) || !File.Exists(submoduleGitPointerPath))
            {
                errors.AppendLine($"Git submodule '{submodule.Name}' @ '{submodule.Path}' does not appear to be checked out, ensure submodules are up-to-date.");
                continue;
            }

            string headRevision = File.ReadAllText(headFilePath).Trim();

            const string namedRefPrefix = "ref: ";
            if (headRevision.StartsWith(namedRefPrefix))
            {
                string namedRef = headRevision.Substring(namedRefPrefix.Length);
                string namedRefFilePath = Path.Combine(gitFolder, namedRef);
                if (!File.Exists(namedRefFilePath))
                {
                    errors.AppendLine($"Could not find named ref for '{submodule.Name}' @ '{submodule.Path}'");
                    continue;
                }

                headRevision = File.ReadAllText(namedRefFilePath).Trim();
            }

            if (submodule.Url.Contains("bonsai-rx/docfx-tools"))
            {
                if (headRevision != "5b584cadc5f1e3088f4972469243df2e3faf6925")
                    errors.AppendLine("docfx-tools is out of date.");

                const string modernDocFxToolsLocaiton = "docs/bonsai-docfx";
                if (submodule.Path != modernDocFxToolsLocaiton)
                    errors.AppendLine($"docfx-tools should be located at '{modernDocFxToolsLocaiton}'");
            }

            if (submodule.Url.Contains("bonsai-rx/docfx-assets"))
                errors.AppendLine($"Git submodule '{submodule.Name}' at '{submodule.Path}' is the deprecated docfx assets repo.");
        }

        return errors.Length > 0 ? errors.ToString() : null;
    }
}
