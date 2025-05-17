using System.IO;

namespace Prefect;

internal class FileExistsRule : FileRule
{
    public override string Description => $"File '{RelativePath}' exists";

    public virtual bool AllowFixupOverwrite => false;

    public FileExistsRule(string relativePath, FileInfo? referenceFile)
        : base(relativePath, referenceFile)
    { }

    protected override string? Validate(Repo repo, string fullFilePath, string relativeFilePath)
        => File.Exists(fullFilePath) ? null : $"File '{relativeFilePath}' must exist.";

    protected override bool Fixup(Repo repo, string fullFilePath, string relativeFilePath)
    {
        if (ReferenceFile is null || ReferenceFile.Length == 0)
            return false;

        if (!AllowFixupOverwrite && File.Exists(fullFilePath))
            return false;

        ReferenceFile.CopyTo(fullFilePath, overwrite: AllowFixupOverwrite);
        return true;
    }
}
