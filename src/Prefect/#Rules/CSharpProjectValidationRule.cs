using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Prefect;

internal sealed class CSharpProjectValidationRule : Rule
{
    public override string Description => "Validate all C# projects.";

    private static readonly ImmutableHashSet<string> ForbiddenProperties =
    [
        // Slightly aggressive checks that mostly exist for tidying, could be removed
        "IsPackable",
        "ToolCommandName",

        // Misc things that generally should not be specified per-project
        "Configuration",
        "Platform",
        "GenerateResourceUsePreserializedResources",
        "EnableWindowsTargeting",
        "BonsaiExecutablePath",

        // Output settings
        "PackageOutputPath",
        "GeneratePackageOnBuild",
        "GenerateDocumentationFile",
        "UseArtifactsOutput",
        "ArtifactsPath",
        "IncludeSymbols",
        "SymbolPackageFormat",

        // Compiler settings that should be global
        "AllowUnsafeBlocks",
        "Features",
        "LangVersion",

        // NuGet package/Assembly metadata
        "Version",
        "VersionPrefix",
        "VersionSuffix",
        "PackageVersion",
        "FileVersion",
        "Authors",
        "Copyright",
        "PackageId",
        "PackageProjectUrl",
        "PackageLicenseFile",
        "PackageLicenseUrl",
        "PackageIcon",
        "PackageIconUrl",
        "PackageReadmeFile",
        "PackageRequireLicenseAcceptance",
        "RepositoryUrl",
        "RepositoryType",
        "PublishRepositoryUrl",
        "EmbedUntrackedSources",
        "PackageType",

        // Easy indications of legacy csproj format
        "ProjectGuid",
        "RootNamespace",
    ];

    private static readonly ImmutableHashSet<string> ForbiddenTestProperties =
    [
        ..ForbiddenProperties,

        // Tests aren't packed or distributed so they shouldn't have any metadata whatsoever
        "Title",
        "Description",
        "PackageTags",
    ];

    private static readonly ImmutableHashSet<string> PermittedTargetFrameworks =
    [
        "net462",
        "net472",
        //"net48", // Only Bonsai's bootstrapper should be using .NET 4.8
        "netstandard2.0",
        "net8.0",
        "net8.0-windows",
    ];

    private static readonly ImmutableHashSet<string> PermittedTargetFrameworksForTests =
    [
        // The `dotnet test` command is limited to being able to filter to either one target framework or have no filter at all
        // Requiring specific frameworks for testing simplifies the common CI pipeline
        "net472",
        "net8.0",
        "net8.0-windows",
    ];

    public override string? Validate(Repo repo)
    {
        StringBuilder errors = new();

        foreach (string projectFilePath in repo.EnumerateFiles("*.csproj"))
        {
            string projectName = Path.GetFileNameWithoutExtension(projectFilePath);
            bool isTestProject = projectName.EndsWith(".Tests");

            if (projectName == "Extensions")
                continue;

            // Make everything lowercase so we can do a case-insensitive matches
            // (Unfortunately System.Xml.XPath doesn't support just turning off case sensitivity.)
            string rawXml = File.ReadAllText(projectFilePath).ToLowerInvariant();
            XDocument xml = XDocument.Parse(rawXml);

            bool firstOfFile = true;
            void EnsureHeader()
            {
                if (firstOfFile)
                {
                    errors.AppendLine($"'{Path.GetRelativePath(repo.RootPath, projectFilePath)}' contains problems:");
                    firstOfFile = false;
                }
            }

            XElement? Select(string xPath)
                => xml.XPathSelectElement(xPath.ToLowerInvariant());

            void Fail(string failureMessage)
            {
                EnsureHeader();
                errors.AppendLine($"    {failureMessage}");
            }

            void Check(string forbiddenXPath, string failureMessage)
            {
                if (Select(forbiddenXPath) is null)
                    return;

                Fail(failureMessage);
            }

            foreach (string forbiddenProperty in isTestProject ? ForbiddenTestProperties : ForbiddenProperties)
                Check($"//PropertyGroup/{forbiddenProperty}", $"Forbidden property '{forbiddenProperty}'");

            // This is a little aggressive, mostly want this for auditing while we tidy things. Could be removed
            Check("/Project/Import", "Projects should not have explicit imports.");

            switch (projectName)
            {
                // Third-party embedded licenses
                case "Bonsai.TensorFlow.MoveNet":
                case "Bonsai.Pylon":
                    break;
                default:
                    Check("//ItemGroup/*[contains(@Include, 'LICENSE')]", "Projects should not embed the license file explicitly");
                    break;
            }
            Check("//ItemGroup/*[contains(@Include, 'icon.png')]", "Projects should not embed the icon file explicitly");

            // https://learn.microsoft.com/en-us/dotnet/core/tools/sdk-errors/netsdk1137
            Check("//Project[@Sdk='Microsoft.NET.Sdk.WindowsDesktop']", "Projects should not use Microsoft.NET.Sdk.WindowsDesktop");

            XElement? targetFrameworksElement = Select("//Project/PropertyGroup/TargetFramework") ?? Select("//Project/PropertyGroup/TargetFrameworks");
            if (targetFrameworksElement is null)
            {
                EnsureHeader();
                Fail("Projects must specify target framework(s).");
            }
            else
            {
                string[] targetFrameworks = targetFrameworksElement.Value.Split(';', System.StringSplitOptions.RemoveEmptyEntries);
                foreach (string _targetFramework in targetFrameworks)
                {
                    string targetFramework = _targetFramework.Trim();
                    if (!PermittedTargetFrameworks.Contains(targetFramework))
                        Fail($"Target framework '{targetFramework}' is not permitted.");
                    else if (isTestProject && !PermittedTargetFrameworksForTests.Contains(targetFramework))
                        Fail($"Target framework '{targetFramework}' is not permitted for tests.");
                }
            }
        }

        return errors.Length > 0 ? errors.ToString() : null;
    }
}
