using System.IO;

namespace Prefect;

internal sealed class DirectoryRule : Rule
{
    public string RelativePath { get; }

    public override string Description => $"Directory '{RelativePath}' exists";

    public DirectoryRule(string relativePath)
        => RelativePath = relativePath;

    public override string? Validate(Repo repo)
    {
        string fullPath = repo.GetFullPath(RelativePath, out string repoRelativePath);
        if (Directory.Exists(fullPath))
            return null;

        return $"Directory '{repoRelativePath}' not found.";
    }

    public override bool Fixup(Repo repo)
    {
        if (!repo.HasValidProjectName && RelativePath.Contains("$PROJECT$"))
            return false;

        string fullPath = repo.GetFullPath(RelativePath, out string repoRelativePath);
        Directory.CreateDirectory(fullPath);
        return true;
    }
}
