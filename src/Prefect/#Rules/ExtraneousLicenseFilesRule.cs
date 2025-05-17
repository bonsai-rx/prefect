using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Prefect;

internal sealed partial class ExtraneousLicenseFilesRule : Rule
{
    public override string Description => "The only license file must be the LICENSE file in the root.";

    // This is loosely based on the logic used by licensee, which is the library GitHub uses for license identification
    // https://github.com/licensee/licensee/blob/c96beb17be6ca7ef735063b779455f8b21dc0ee6/lib/licensee/project_files/license_file.rb
    [GeneratedRegex(@"(^|[-_\.])((UN)?LICEN[SC]E|COPY(ING|RIGHT)|OFL|PATENTS)($|[-_\.])", RegexOptions.IgnoreCase)]
    private static partial Regex LicenseFileNameRegex();

    public override string? Validate(Repo repo)
    {
        StringBuilder errors = new();

        foreach (string filePath in repo.EnumerateFiles("*"))
        {
            string relativePath = PathEx.GetNormalRelativeTo(repo.RootPath, filePath);

            if (relativePath == "LICENSE")
                continue;

            // Check if this license is in an external dependency
            if (relativePath.StartsWith("src/Externals/"))
                continue;

            // Check if this is a license in a submodule
            if (Path.GetFileName(filePath) == "LICENSE" && File.Exists(Path.Combine(Path.GetDirectoryName(filePath)!, ".git")))
                continue;

            string bareName = Path.GetFileNameWithoutExtension(filePath).ToUpperInvariant();
            if (LicenseFileNameRegex().IsMatch(bareName))
                errors.AppendLine($"'{relativePath}' appears to be a non-conformant license file.");
        }

        return errors.Length > 0 ? errors.ToString() : null;
    }
}
