using System;
using System.IO;
using System.Text;

namespace Prefect;

internal sealed class InterpolatedFileContentRule : FileExistsRule
{
    public string Template { get; }
    public override string Description => $"File '{RelativePath}' has expected dynamic contents";

    public bool AlwaysOverwrite { get; set; } = true;
    public override bool AllowFixupOverwrite => AlwaysOverwrite;

    public InterpolatedFileContentRule(string relativePath, FileInfo referenceFile)
        : base(relativePath, referenceFile)
    {
        using StreamReader f = new(referenceFile.OpenRead());
        Template = f.ReadToEnd().ReplaceLineEndings("\n");
    }

    private string? GetInterpolatedContent(Repo repo, StringBuilder errors)
    {
        bool hasErrors = false;
        string result = repo.EvaluateTemplate
        (
            Template,
            (variable) =>
            {
                errors.AppendLine($"Template '{RelativePath}' contains unknown variable '{variable}'");
                hasErrors = true;
                return variable.ToString();
            }
        );

        return hasErrors ? null : result;
    }

    protected override string? Validate(Repo repo, string fullFilePath, string relativeFilePath)
    {
        if (base.Validate(repo, fullFilePath, relativeFilePath) is string failReason)
            return failReason;

        if (!AllowFixupOverwrite)
            return null;

        string actualContents;
        using (StreamReader f = new(File.OpenRead(fullFilePath)))
            actualContents = f.ReadToEnd().ReplaceLineEndings("\n");

        StringBuilder errors = new();
        string? expectedContents = GetInterpolatedContent(repo, errors);

        if (expectedContents is null)
        {
            errors.AppendLine($"Cannot verify '{relativeFilePath}' due to errors in reference template.");
            return errors.ToString();
        }

        if (actualContents.Equals(expectedContents, StringComparison.Ordinal))
            return null;
        else
            return $"Contents of '{relativeFilePath}' do not match the reference.";
    }

    protected override bool Fixup(Repo repo, string fullFilePath, string relativeFilePath)
    {
        if (!AllowFixupOverwrite && File.Exists(fullFilePath))
            return false;

        // Determine existing file line endings
        bool useCrlf = OperatingSystem.IsWindows();
        if (File.Exists(fullFilePath))
        {
            using StreamReader f = new(File.OpenRead(fullFilePath));
            while (!f.EndOfStream)
            {
                int c = f.Read();
                if (c == '\r' && f.Peek() == '\n')
                {
                    useCrlf = true;
                    break;
                }
                else if (c == '\n')
                {
                    useCrlf = false;
                    break;
                }
            }
        }

        StringBuilder errors = new();
        string? newContents = GetInterpolatedContent(repo, errors);
        if (newContents is null)
            return false;

        if (useCrlf)
            newContents = newContents.ReplaceLineEndings("\r\n");

        File.WriteAllText(fullFilePath, newContents);
        return true;
    }
}
