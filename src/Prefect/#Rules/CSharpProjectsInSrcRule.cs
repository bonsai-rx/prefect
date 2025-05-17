using System.IO;
using System.Text;

namespace Prefect;

internal sealed class CSharpProjectsInSrcRule : Rule
{
    public override string Description => "All csproj files are contained within the src directory";

    public override string? Validate(Repo repo)
    {
        StringBuilder errors = new();

        string srcDirectoryPath = Path.Combine(repo.RootPath, "src");
        foreach (string csprojPath in repo.EnumerateFiles("*.csproj"))
        {
            string relativeToSrc = PathEx.GetNormalRelativeTo(srcDirectoryPath, csprojPath);

            // Right now this exception exists primarily for bonsai-rx/machinelearning
            // Need to decide if this is something we want to actually allow/encourage
            // https://github.com/bonsai-rx/prefect/issues/14
            if (csprojPath.EndsWith(".Tests.csproj") && relativeToSrc.StartsWith("../tests/"))
                continue;

            if (Path.GetFileName(csprojPath) == "Extensions.csproj")
                continue;

            if (relativeToSrc.StartsWith(".."))
                errors.AppendLine($"'{Path.GetRelativePath(repo.RootPath, csprojPath)}' is not within the 'src' directory.");
        }

        return errors.Length > 0 ? errors.ToString() : null;
    }
}
