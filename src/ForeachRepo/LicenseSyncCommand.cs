using System;
using System.Collections.Immutable;
using System.IO;

namespace ForeachRepo;

internal sealed class LicenseSyncCommand : CommandBase
{
    public LicenseSyncCommand()
        => LongOutput = false;

    public override void Run(Context context, ImmutableArray<string> args)
    {
        const string licenseFileName = "LICENSE";
        if (!File.Exists(licenseFileName))
        {
            Console.Error.WriteLine("License does not exist, not going to add one either.");
            return;
        }

        string oldText = File.ReadAllText(licenseFileName).ReplaceLineEndings();
        if (oldText == LicenseText)
        {
            Console.WriteLine("Already up-to-date");
            return;
        }

        using (StreamWriter writer = new(licenseFileName))
        {
            writer.Write(LicenseText);

            // Handle writing out any other licenses stapled to the old one
            int indexOfOtherPartBegin = oldText.IndexOf("-----");
            if (indexOfOtherPartBegin >= 0)
            {
                ReadOnlySpan<char> otherPart = oldText.AsSpan().Slice(indexOfOtherPartBegin);
                otherPart = otherPart.TrimStart('-').TrimStart();

                writer.WriteLine();
                writer.WriteLine();
                writer.WriteLine(StandardDivider);
                writer.WriteLine();
                writer.Write(otherPart);
            }
        }
        Console.WriteLine("Synchronized!");
    }

    private static readonly string StandardDivider =
        "-------------------------------------------------------------------------------";
    private static readonly string LicenseText =
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
        """.ReplaceLineEndings();
}
