using System;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.Linq;

namespace Prefect
{
    internal class ExtendedHelpAction(HelpAction defaultHelp) : SynchronousCommandLineAction
    {
        private static void WriteHelp(string helpText)
        {
            int absoluteLineLength = 80;
            try
            { absoluteLineLength = Math.Max(40, Console.BufferWidth); }
            catch (PlatformNotSupportedException)
            { }

            foreach (string _line in helpText.Split('\n'))
            {
                ReadOnlySpan<char> line = _line.AsSpan().TrimEnd();
                ReadOnlySpan<char> message = line.TrimStart(' ');
                ReadOnlySpan<char> indent = line.Slice(0, line.Length - message.Length);
                int lineLength = absoluteLineLength - indent.Length;

                do
                {
                    ReadOnlySpan<char> messagePart = message;
                    if (messagePart.Length > lineLength)
                    {
                        messagePart = messagePart.Slice(0, lineLength);
                        int wordBoundary = messagePart.LastIndexOf(' ');
                        if (wordBoundary > 0)
                            messagePart = messagePart.Slice(0, wordBoundary);
                    }

                    Console.WriteLine($"{indent}{messagePart}");
                    message = message.Slice(messagePart.Length).TrimStart(' ');
                }
                while (message.Length > 0);
            }
        }

        public override int Invoke(ParseResult parseResult)
        {
            string preHelpText =
                $"""
            ______
           (, /   )         /)
            _/__ / __   _  //  _  _ _/_
            /     / (__(/_/(__(/_(__(__
         ) /             /)
        (_/             (/

        """;

            WriteHelp(preHelpText);
            int result = defaultHelp.Invoke(parseResult);

            string postHelpText =
                $"""
        Reference template:
            A path to a directory tree to be used as the reference template.
            For each file and directory in the tree, Prefect will validate the file exists in the target.
            For files which have contents, Prefect will also validate that the contents match.
            Reference templates may also contain additional configuration files to control Prefect's behavior, as described in detail below.

        Template configuration files:
            .prefect-template-kind
                Contains a well-known template kind used to add additional programmatic rules to the rule set.
                Supported values are: {string.Join(", ", Enum.GetValues<TemplateKind>().Where(x => x != TemplateKind.None).Select(x => x.ToString()))}

            .prefect-ignore-content
                An index of file paths which are permitted to diverge in the target repo.
                The contents of the file in the template will be used only if the file does not yet exist in the target.

            .prefect-interpolated-files
                An index of file paths in the template which will have their contents interpolated before validation.

            .prefect-must-not-exist
                An index of file paths which must not exist in the target, typically used for legacy files which are no longer desired.

        File index format:
            File indices list one file path per line.
            Indentation and any trailing whitespace is automatically trimmed.
            Blank lines and lines beginning with `#` are ignored.
            Glob syntax is *not* supported.

        Interpolation:
            The names of all files and the contents of files listed in `.prefect-interpolated-files` will be interpolated using the special $INTERPOLATION$ syntax.

            The following interpolations are supported:
                $PROJECT$ - The human-friendly name of a project (typically the package name.)
                $REPO-SLUG$ - The name of the repository, which is the name of the folder which contains it.

        Project names:
            The project name corresponding to a repository is inferred unless specified using `--project-name`.
            By default, the name of a project will be the name of the shortest-named `.sln` file in the root of the repository.
            When using a {nameof(TemplateKind.HarpTech)} template, the `Interface` directory is used instead of the root.

        Exit code:
            Prefect will exit with a non-zero exit code if any repository failed validation or if the specified repository set is effectively empty.
        """;

            WriteHelp(postHelpText);
            return result;
        }
    }
}
