using System.IO;
using System.Text;
using System.Xml.Linq;

namespace Prefect;

internal sealed class BuildInfrastructureOmitsUnecessaryNoiseRule : Rule
{
    public override string Description => "Build infrastructure does not contain any unecessary noise.";

    public override string? Validate(Repo repo)
    {
        StringBuilder errors = new();

        foreach (string filePath in repo.EnumerateFiles())
        {
            switch (Path.GetExtension(filePath).ToLowerInvariant())
            {
                case ".props":
                case ".targets":
                case ".csproj":
                    break;
                default:
                    continue;
            }

            string relativePath = Path.GetRelativePath(repo.RootPath, filePath);
            if (relativePath.StartsWith($"artifacts{Path.DirectorySeparatorChar}"))
                continue;

            XDocument xml = XDocument.Load(filePath);

            if (xml.Declaration is not null)
                errors.AppendLine($"'{relativePath}' contains an XML declaration.");

            if (xml.Root is XElement { Name.LocalName: "Project" } projectRoot)
            {
                foreach (XAttribute attribute in projectRoot.Attributes())
                {
                    switch (attribute.Name.LocalName.ToLowerInvariant())
                    {
                        case "ToolsVersion":
                        case "xmlns":
                            errors.AppendLine($"'{relativePath}' contains legacy attribute '{attribute.Name}'");
                            break;
                    }
                }
            }
            else
            { errors.AppendLine($"'{relativePath}' does not seem to be an MSBuild file."); }
        }

        return errors.Length > 0 ? errors.ToString() : null;
    }
}
