using System.IO;
using System.Text;

namespace Prefect;

internal sealed class NoBuildStuffOutsideOfBuildFolderRule : Rule
{
    public override string Description => "Build infrastructure is fully contained within the build folder.";

    public override string? Validate(Repo repo)
    {
        StringBuilder errors = new();

        string buildDirectoryPath = Path.Combine(repo.RootPath, "build");
        foreach (string filePath in repo.EnumerateFiles())
        {
            switch (Path.GetExtension(filePath).ToLowerInvariant())
            {
                case ".props":
                case ".targets":
                    break;
                default:
                    continue;
            }

            string relativePath = Path.GetRelativePath(repo.RootPath, filePath);
            switch (relativePath)
            {
                case "Directory.Build.props":
                case "Directory.Build.targets":
                case "Directory.Packages.props":
                    continue;
            }

            if (relativePath.StartsWith($"artifacts{Path.DirectorySeparatorChar}"))
                continue;

            string relativeToBuild = Path.GetRelativePath(buildDirectoryPath, filePath);
            if (relativeToBuild.StartsWith(".."))
                errors.AppendLine($"'{Path.GetRelativePath(repo.RootPath, filePath)}' is outside of the 'build' directory.");
        }

        return errors.Length > 0 ? errors.ToString() : null;
    }
}
