using System.IO;

namespace Prefect;

internal abstract class FileRule : Rule
{
    public string RelativePath { get; }
    public FileInfo? ReferenceFile { get; }

    public FileRule(string relativePath, FileInfo? referenceFile)
    {
        RelativePath = relativePath;
        ReferenceFile = referenceFile;
    }

    public sealed override string? Validate(Repo repo)
    {
        string fullPath = repo.GetFullPath(RelativePath, out string relativePath);
        return Validate(repo, fullPath, relativePath);
    }

    protected abstract string? Validate(Repo repo, string fullFilePath, string relativeFilePath);

    public sealed override bool Fixup(Repo repo)
    {
        if (!repo.HasValidProjectName && RelativePath.Contains("$PROJECT$"))
            return false;

        string fullPath = repo.GetFullPath(RelativePath, out string relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        return Fixup(repo, fullPath, relativePath);
    }

    protected virtual bool Fixup(Repo repo, string fullFilePath, string relativeFilePath)
        => false;
}
