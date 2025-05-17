using System;
using System.Text;

namespace Prefect;

internal sealed class FileGlobDoesNotExistRule : Rule
{
    public string Subdirectory { get; }
    public string Pattern { get; }
    public override string Description => Subdirectory.Length > 0 ? $"Files matching '{Pattern}' under '{Subdirectory}' do not exist" : $"Files matching '{Pattern}' do not exist";

    public Func<Repo, string, bool>? IsException;

    public FileGlobDoesNotExistRule(string subdirectory, string pattern, Func<Repo, string, bool>? isException = null)
    {
        Subdirectory = subdirectory;
        Pattern = pattern;
        IsException = isException;

        // We want to add full glob pattern support eventually but for now it's just the basics supported by Directory.EnumerateFiles
        if (Pattern.Contains("**") || Pattern.Contains('/') || Pattern.Contains('\\'))
            new NotSupportedException("Glob support is absurdly basic right now, glob not supported!");
    }

    public FileGlobDoesNotExistRule(string pattern, Func<Repo, string, bool>? isException = null)
        : this("", pattern, isException)
    { }

    public override string? Validate(Repo repo)
    {
        StringBuilder errors = new();

        foreach (string filePath in repo.EnumerateFiles(Subdirectory, Pattern))
        {
            string relativePath = PathEx.GetNormalRelativeTo(repo.RootPath, filePath);

            if (IsException?.Invoke(repo, relativePath) == true)
                continue;

            errors.AppendLine($"File '{relativePath}' matches '{Pattern}' which must not exist.");
        }

        return errors.Length > 0 ? errors.ToString() : null;
    }
}
