using System;
using System.IO;
using System.Security.Cryptography;

namespace Prefect;

internal sealed class FileContentRule : FileExistsRule
{
    private readonly byte[] _ExpectedHash;
    public ReadOnlySpan<byte> ExpectedHash => _ExpectedHash;

    public override bool AllowFixupOverwrite => true;

    public override string Description => $"File '{RelativePath}' has expected contents";

    public FileContentRule(string relativePath, FileInfo referenceFile)
        : base(relativePath, referenceFile)
    {
        using FileStream f = referenceFile.OpenRead();
        _ExpectedHash = SHA256.HashData(f);
    }

    protected override string? Validate(Repo repo, string fullFilePath, string relativeFilePath)
    {
        if (base.Validate(repo, fullFilePath, relativeFilePath) is string failReason)
            return failReason;

        using Stream actualFile = File.OpenRead(fullFilePath);
        ReadOnlySpan<byte> actualHash = SHA256.HashData(actualFile);
        return actualHash.SequenceEqual(ExpectedHash) ? null : $"Contents of '{relativeFilePath}' do not match the reference.";
    }
}
