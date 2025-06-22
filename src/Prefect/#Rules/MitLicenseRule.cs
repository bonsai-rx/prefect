using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Prefect;

internal sealed partial class MitLicenseRule : FileExistsRule
{
    public override string Description => $"{RelativePath} contains the appropriate license";
    private EnforcementLevel StrictEnforcement;

    public enum EnforcementLevel
    {
        /// <summary>License must be MIT, don't check copyright string or exact formatting.</summary>
        MustBeMitLicense,
        /// <summary>License must be MIT and copyright Bonsai Foundation, don't check copyright year or exact formatting.</summary>
        MustBeBonsaiFoundation,
        /// <summary>License must match the standard Bonsai Foundation license exactly, excluding any third-party notices.</summary>
        StrictFormatting,
    }

    public MitLicenseRule(EnforcementLevel enforcementLevel = EnforcementLevel.StrictFormatting)
        : base("LICENSE", null)
        => StrictEnforcement = enforcementLevel;

    protected override string? Validate(Repo repo, string fullFilePath, string relativeFilePath)
    {
        if (base.Validate(repo, fullFilePath, relativeFilePath) is string failReason)
            return failReason;

        string actualContent = File.ReadAllText(fullFilePath);

        if (StrictEnforcement >= EnforcementLevel.StrictFormatting)
        {
            actualContent = actualContent.ReplaceLineEndings("\n");

            if (!actualContent.StartsWith(StrictLicenseContent))
                return $"License file '{relativeFilePath}' is not the correct license format.";

            ReadOnlySpan<char> remaining = actualContent.AsSpan().Slice(StrictLicenseContent.Length);

            if (remaining.Length == 0)
            { } // All good, nothing to do
            else if (remaining.StartsWith(LicenseDivider))
            { } // All good, extra licenses are permitted
            else if (remaining.Trim().Length == 0)
            { return $"License file '{relativeFilePath}' ends with extraneous whitespace."; }
            else if (remaining.TrimStart().StartsWith("--"))
            { return $"License file '{relativeFilePath}' seems to be followed by an additional license but the separator is incorrect."; }
            else
            { return $"License file '{relativeFilePath}' has additional text after the license without any sort of separation."; }
        }
        else
        {
            actualContent = actualContent.ReplaceLineEndings(" ");
            Match match = ExpectedLicenseRegex().Match(actualContent);

            if (!match.Success)
                return $"License file '{relativeFilePath}' does not appear to be an MIT license.";

            if (StrictEnforcement >= EnforcementLevel.MustBeBonsaiFoundation)
            {
                Debug.Assert(match.Index == 0 && match.Length == actualContent.Length);

                ReadOnlySpan<char> actualCopyrightAttribution = match.Groups["copyrightAttribution"].ValueSpan;
                if (!actualCopyrightAttribution.SequenceEqual(ExpectedCopyrightAttribution))
                    return $"License file '{relativeFilePath}' contains attribution to '{actualCopyrightAttribution}' rather than '{ExpectedCopyrightAttribution}'.";
            }
        }

        return null;
    }

    private const string ExpectedCopyrightAttribution = "Bonsai Foundation CIC and Contributors";

    [GeneratedRegex(
        @"^((The MIT License \(MIT\)|MIT License)  )?" +
        @"Copyright \([cC]\) (\d{4}(-\d{4})? )?(?<copyrightAttribution>.+?) " +
        @" " +
        @"Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files \(the ""Software""\), to deal in " +
        @"the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and\/or sell copies of the " +
        @"Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: " +
        @" " +
        @"The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. " +
        @" " +
        @"THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR " +
        @"A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN " +
        @"ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE." +
        // Ignore trialing whitespace
        @"\s*?" +
        // Ignore extra licenses concatenated to the main license (sometimes used for third-party dependencies)
        @"(?<thirdPartyLicenses>\s-{5,} .+)?" +
        // Must match entire file
        @"$"
    )]
    private static partial Regex ExpectedLicenseRegex();

    private static readonly string StrictLicenseContent =
        """
        Copyright (c) Bonsai Foundation CIC and Contributors

        Permission is hereby granted, free of charge, to any person obtaining a copy of
        this software and associated documentation files (the "Software"), to deal in
        the Software without restriction, including without limitation the rights to
        use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
        of the Software, and to permit persons to whom the Software is furnished to do
        so, subject to the following conditions:

        The above copyright notice and this permission notice shall be included in all
        copies or substantial portions of the Software.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
        SOFTWARE.
        """.ReplaceLineEndings("\n");

    private static readonly string LicenseDivider = "\n\n-------------------------------------------------------------------------------\n\n";

    protected override bool Fixup(Repo repo, string fullFilePath, string relativeFilePath)
        // Intentionally not implemented, this shouldn't be performed automatically!
        => false;

    /// <summary>Returns true if the specified license text is a plain MIT license with no third-party notices attached.</summary>
    public static bool IsPlainMitLicense(string licensePath)
    {
        string licenseText = File.ReadAllText(licensePath);
        licenseText = licenseText.ReplaceLineEndings(" ");
        Match match = ExpectedLicenseRegex().Match(licenseText);
        return match.Success && !match.Groups["thirdPartyLicenses"].Success;
    }
}
