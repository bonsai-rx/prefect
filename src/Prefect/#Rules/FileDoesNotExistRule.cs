using System.IO;

namespace Prefect;

internal sealed class FileDoesNotExistRule : FileRule
{
    public override string Description => $"File '{RelativePath}' does not exist";

    public FileDoesNotExistRule(string relativePath)
        : base(relativePath, null)
    { }

    protected override string? Validate(Repo repo, string fullFilePath, string relativeFilePath)
        => File.Exists(fullFilePath) ? $"File '{relativeFilePath}' must not exist." : null;
}
