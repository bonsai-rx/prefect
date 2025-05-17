using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Prefect;

internal sealed class BonsaiFoundationPackageMetadataRule : Rule
{
    public override string Description => $"NuGet Package metadata is correct.";

    public override string? Validate(Repo repo)
    {
        StringBuilder errors = new();

        // Validate global package metadata
        {
            string relativePath = "build/Package.props";
            string filePath = Path.Combine(repo.RootPath, relativePath);

            if (!File.Exists(filePath))
            { errors.AppendLine($"'{relativePath}' does not exist."); }
            else
            {
                XDocument xml = XDocument.Load(filePath);

                string expectedUrl = $"https://bonsai-rx.org/{repo.RepoSlug}";
                if (xml.XPathSelectElement("/Project/PropertyGroup/PackageProjectUrl")?.Value != expectedUrl)
                    errors.AppendLine($"'{relativePath}': PackageProjectUrl should be '{expectedUrl}'");

                const string expectedCopyright = "Copyright © Bonsai Foundation CIC and Contributors";
                if (xml.XPathSelectElement("/Project/PropertyGroup/Copyright")?.Value != expectedCopyright)
                    errors.AppendLine($"'{relativePath}': Copyright should be '{expectedCopyright}'");
            }
        }

        // Validate per-project package metadata
        foreach (string projectFilePath in repo.EnumerateFiles("*.csproj"))
        {
            if (Path.GetFileNameWithoutExtension(projectFilePath).EndsWith(".Tests"))
                continue;

            string relativePath = Path.GetRelativePath(repo.RootPath, projectFilePath);

            XDocument xml = XDocument.Load(projectFilePath);

            string bonsaiPrefix = "Bonsai - ";

            //TODO: Does this really make sense?
            // Should tools even have titles and descriptions? I don't think either show up in any common place people use to interact with .NET tools
            if (xml.XPathSelectElement("/Project/PropertyGroup/PackAsTool")?.Value == "true")
                bonsaiPrefix = "Bonsai ";

            if (repo.ProjectName != "Bonsai.ML") // Project is inconsistent in this regard.
            {
                if (xml.XPathSelectElement("/Project/PropertyGroup/Title")?.Value?.StartsWith(bonsaiPrefix) != true)
                    errors.AppendLine($"'{relativePath}': Title should be in the form of '{bonsaiPrefix}Project Name'");
            }

            // The description may not show up on NuGet.org anymore when a readme is present, but it's still shown in clients (including Bonsai)
            if (xml.XPathSelectElement("/Project/PropertyGroup/Description")?.Value is null or "")
                errors.AppendLine($"'{relativePath}': The package should have a description.");

            // If PackageTags is present, it should build off of the global tags
            string? packageTags = xml.XPathSelectElement("/Project/PropertyGroup/PackageTags")?.Value;
            if (packageTags is not null && !packageTags.StartsWith("$(PackageTags) "))
                errors.AppendLine($"'{relativePath}': Projects should only add package tags, not replace them.");
        }

        return errors.Length > 0 ? errors.ToString() : null;
    }
}
