using Prefect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

//-------------------------------------------------------------------------------------------------
// Command line parsing
//-------------------------------------------------------------------------------------------------
string? templatePath = null;
List<string> repoArguments = new();
HashSet<string> skipNames = new();
HashSet<string> skipFullPaths = new();
string? projectNameOverride = null;
bool interactiveMode = false;
bool enableAutomaticFixes = false;
bool showHelp = false;
{
    Queue<string> arguments = new(args);

    if (arguments.Count == 0)
        showHelp = true;

    while (arguments.Count > 0)
    {
        string argument = arguments.Dequeue();
        switch (argument.ToLowerInvariant())
        {
            case "--skip":
            {
                string skipArg = arguments.Dequeue();
                skipNames.Add(skipArg);
                skipFullPaths.Add(Path.GetFullPath(skipArg));
                break;
            }
            case "--interactive":
                interactiveMode = true;
                break;
            case "--auto-fix":
                enableAutomaticFixes = true;
                break;
            case "--project-name":
                if (projectNameOverride is not null)
                {
                    Console.Error.WriteLine("--project-name cannot be specified more than once!");
                    Console.Error.WriteLine();
                    showHelp = true;
                }

                projectNameOverride = arguments.Dequeue();
                break;
            case "--help":
            case "-help":
            case "/help":
            case "-h":
            case "/h":
            case "-?":
            case "/?":
                showHelp = true;
                break;
            default:
                if (templatePath is null)
                    templatePath = Path.GetFullPath(argument);
                else
                    repoArguments.Add(Path.GetFullPath(argument));
                break;
        }
    }
}

//-------------------------------------------------------------------------------------------------
// Help
//-------------------------------------------------------------------------------------------------
if (showHelp)
{
    string helpText =
        $"""
            ______
           (, /   )         /)
            _/__ / __   _  //  _  _ _/_
            /     / (__(/_/(__(/_(__(__
         ) /             /)
        (_/             (/

        Usage:
            prefect <reference-template> [repo ...] [--skip repo] [--interactive] [--auto-fix] [--project-name name]

        Arguments and flags:
            <reference-template>
                Required path to the reference template to use for validaiton (see detailed description below.)

            [repo ...]
                The repository or set of repositories to validate. Multiple can be specified.

                If the specified path is a Git repository, then that single repository will be added to the validatation set.
                If the specified path is a non-Git directory, then each Git repository directly under it is added to the validation set.

                If no repositories are specified, the list of configured validation rules will be printed.

            --skip repo
                Indicates that the specified repository should be skipped, can be specified multiple times.
                `repo` can be either the name of the repository's folder, the path to a repository, or the repository's project name.

            --interactive
                Enables interactive mode
                In interactive mode Prefect will pause and wait for the user to correct issues whenever a repository fails validation, after which the repository will be checked again.

            --auto-fix
                When enabled, Prefect will automatically fix certain rule violations. (Note that fixes may be destructive.)

            --project-name name
                Overrides the project name instead of using automatic detection, cannot be used with sets of multiple repositories.
                This flag is most useful when provisioning new repositories where the automatic name detection has nothing to work with.

        Reference template:
            A path to a directory tree to be used as the reference template.
            For each file and directory in the tree, Prefect will validate the file exists in the target.
            For files which have contents, Prefect will also validate that the contents match.
            Reference templates may also contain additional configuration files to control Prefect's behavior, as described in detail below.

        Template configuration files:
            .prefect-template-kind
                Contains a well-known template kind used to add additional programmatic rules to the rule set.
                Supported values are: {String.Join(", ", Enum.GetValues<TemplateKind>().Where(x => x != TemplateKind.None).Select(x => x.ToString()))}

            .prefect-ignore-content
                An index of file paths which are permitted to diverge in the target repo.
                The contents of the file in the template will be used only if the file does not yet exist in the target.

            .prefect-interpolated-files
                An index of file paths in the template which will have their contents interpolated before validation.

            .prefect-must-not-exist
                An index of file paths which must not exist in the target, typically used for legacy files which are not longer desired.

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
            Prefect will exit with a non-zero exit code if any repository failed validaiton or if the specified repository set is effectively empty.
        """;

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

    return 0;
}

//-------------------------------------------------------------------------------------------------
// Execution
//-------------------------------------------------------------------------------------------------
if (templatePath is null)
{
    Console.Error.WriteLine("No reference template was specified.");
    return 1;
}
else if (!Directory.Exists(templatePath))
{
    Console.Error.WriteLine($"The specified template path '{templatePath}' does not exist.");
    return 1;
}

// Load ruleset
Ruleset rules = new(templatePath);

// Print ruleset when there's no repos to validate
if (repoArguments.Count == 0)
{
    Console.WriteLine($"The following rules will be enforced by '{templatePath}':");
    foreach (Rule rule in rules)
        Console.WriteLine($"* {rule.Description}");

    return 0;
}

// Enumerate repositories to validate
List<string> validationSet = new();
{
    bool haveErrors = false;

    foreach (string repoArgument in repoArguments)
    {
        if (File.Exists(repoArgument))
        {
            Console.Error.WriteLine($"Path '{repoArgument}' points to a file, repository paths must be directories.");
            haveErrors = true;
            continue;
        }

        if (!Directory.Exists(repoArgument))
        {
            Console.Error.WriteLine($"Path '{repoArgument}' does not exist.");
            haveErrors = true;
            continue;
        }

        // Repo arguments must be normalized full paths
        Debug.Assert(repoArgument == Path.GetFullPath(repoArgument));

        // Handle single Git repo case
        // Do not disable this check, see remarks on IsRepository
        if (Repo.IsRepository(repoArgument))
        {
            validationSet.Add(repoArgument);
            continue;
        }

        // Handle directory of Git repos case
        int added = 0;
        foreach (string nestedRepoPath in Directory.EnumerateDirectories(repoArgument, "*", SearchOption.TopDirectoryOnly))
        {
            // Do not disable this check, see remarks on IsRepository
            if (!Repo.IsRepository(nestedRepoPath))
                continue;

            validationSet.Add(nestedRepoPath);
            added++;
        }

        if (added == 0)
        {
            Console.Error.WriteLine($"Path '{repoArgument}' is not a valid repository or repository set.");
            haveErrors = true;
            continue;
        }
    }

    // We already handled the case of no repo arguments being provided, and each repo argument is expected to ether
    // produce at least one repository to validate or an error.
    if (validationSet.Count == 0 && !haveErrors)
        throw new UnreachableException();

    // Abort if there were errors
    if (haveErrors)
        return 1;
}

// Overriding the project name doesn't make sense if there's multiple repositories to validate
if (validationSet.Count > 1 && projectNameOverride is not null)
{
    Console.Error.WriteLine("Cannot override project name with more than one repository to validate.");
    return 1;
}

// Main validation loop
int failedRepoCount = 0;
int passedRepoCount = 0;
const int maxAutofixAttempts = 3;
ConsoleColor defaultConsoleForegroundColor = Console.ForegroundColor;
foreach (string repoPath in validationSet)
{
    int remainingAutofixAttempts = maxAutofixAttempts;
    Again:
    Repo repo = new(repoPath, rules.Kind, projectNameOverride);

    Console.ForegroundColor = ConsoleColor.White;
    Console.Write($"Validating '{repo.RepoSlug}' ({repo.ProjectName})...");

    bool noFailures = true;
    bool hadFixableErrors = false;
    bool hadUnfixableErrors = false;
    foreach (Rule rule in rules)
    {
        if (rules.ShouldSkip(rule, repo))
            continue;

        if (rule.Validate(repo) is string failReason)
        {
            if (noFailures)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("FAIL");
                Console.ForegroundColor = defaultConsoleForegroundColor;
                noFailures = false;
            }

            if (enableAutomaticFixes && rule.Fixup(repo))
            {
                Console.Write("✨ ");
                hadFixableErrors = true;
            }
            else
            { hadUnfixableErrors = true; }

            Console.WriteLine(failReason.TrimEnd());
        }
    }

    // Handle verdict
    if (noFailures)
    {
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine("PASS");
        Console.ForegroundColor = defaultConsoleForegroundColor;
        passedRepoCount++;
        continue;
    }
    else if (!hadUnfixableErrors && remainingAutofixAttempts > 0)
    {
        Debug.Assert(hadFixableErrors);
        Console.WriteLine("All problems were automatically fixed.");
        remainingAutofixAttempts--;
        goto Again;
    }
    else if (!interactiveMode)
    {
        if (remainingAutofixAttempts == 0)
            Console.WriteLine("Auto-fixing seems to be stuck, not going to try further.");

        Console.WriteLine();
        failedRepoCount++;
        continue;
    }
    else
    {
        Console.WriteLine();

        if (remainingAutofixAttempts == 0)
        {
            Console.WriteLine("Auto-fixing seems to be stuck, please resolve the above issues manually.");
            Console.WriteLine();
        }

        remainingAutofixAttempts = maxAutofixAttempts;
        Console.WriteLine("Fix the above issues and press enter to continue, S to skip, or Escape to abort...");
        while (true)
        {
            ConsoleKey key = Console.ReadKey(intercept: true).Key;
            if (key == ConsoleKey.Enter)
            {
                Console.Clear();
                goto Again;
            }
            else if (key == ConsoleKey.S)
            {
                Console.WriteLine();
                failedRepoCount++;
                break;
            }
            else if (key == ConsoleKey.Escape)
            {
                Console.WriteLine("Aborting validation.");
                return 1;
            }
        }
    }
}

Debug.Assert((passedRepoCount + failedRepoCount) == validationSet.Count);
Console.ForegroundColor = failedRepoCount == 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed;
Console.WriteLine($"{passedRepoCount}/{validationSet.Count} repositories passed validation.");
Console.ForegroundColor = defaultConsoleForegroundColor;

return failedRepoCount > 0 ? 1 : 0;
