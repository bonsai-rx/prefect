using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Prefect;

internal sealed class BonsaiLaunchSettingsJsonRule : Rule
{
    public override string Description => "All 'launchSettings.json' files have the expected content.";

    private static string ExpectedContents =
        """
        {
          "profiles": {
            "Bonsai": {
              "commandName": "Executable",
              "executablePath": "$(BonsaiExecutablePath)",
              "commandLineArgs": "--lib:\"$(TargetDir).\"",
              "nativeDebugging": true
            }
          }
        }
        """.ReplaceLineEndings();

    private IEnumerable<string> EnumerateLaunchJsonPaths(Repo repo)
    {
        foreach (string projectFilePath in repo.EnumerateFiles("*.csproj"))
        {
            // Don't care about tests
            if (projectFilePath.EndsWith(".Tests.csproj", StringComparison.OrdinalIgnoreCase))
                continue;

            // Don't care about Extensions.csproj
            if (Path.GetFileName(projectFilePath).Equals("Extensions.csproj", StringComparison.OrdinalIgnoreCase))
                continue;

            // Don't care about .NET tools
            {
                // Make everything lowercase so we can do a case-insensitive matches
                // (Unfortunately System.Xml.XPath doesn't support just turning off case sensitivity.)
                string rawXml = File.ReadAllText(projectFilePath).ToLowerInvariant();
                XDocument xml = XDocument.Parse(rawXml);
                XElement? packAsTool = xml.XPathSelectElement("/Project/PropertyGroup/PackAsTool".ToLowerInvariant());
                if (packAsTool?.Value == "true")
                    continue;
            }

            yield return Path.Combine(Path.GetDirectoryName(projectFilePath)!, "Properties", "launchSettings.json");
        }
    }

    public override string? Validate(Repo repo)
    {
        StringBuilder errors = new();

        foreach (string launchSettingsPath in EnumerateLaunchJsonPaths(repo))
        {
            string relativeFilePath = Path.GetRelativePath(repo.RootPath, launchSettingsPath);

            if (!File.Exists(launchSettingsPath))
                errors.Append($"\n    File '{relativeFilePath}' must exist.");
            else if (File.ReadAllText(launchSettingsPath).ReplaceLineEndings() != ExpectedContents)
                errors.Append($"\n    Contents of '{relativeFilePath}' do not match the reference.");
        }

        return errors.Length > 0 ? $"Not all 'launchSettings.json' files are correct:{errors}" : null;
    }

    public override bool Fixup(Repo repo)
    {
        foreach (string launchSettingsPath in EnumerateLaunchJsonPaths(repo))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(launchSettingsPath)!);
            File.WriteAllText(launchSettingsPath, ExpectedContents);
        }

        return true;
    }
}
