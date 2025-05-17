using ForeachRepo;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

RunCommand runCommand = new();
Dictionary<string, CommandBase> commands = new()
{
    { "run", runCommand },
    { "git", new WellKnownCommand("git") },
    { "gh", new WellKnownCommand("gh") },

    { "show-head", ("git show HEAD --pretty=oneline --no-patch --decorate --decorate-refs-exclude=refs/tags --decorate-refs-exclude=*/HEAD", false) },
    { "license-diff", ("git diff --merge-base upstream/HEAD LICENSE") },
    { "license-sync", new LicenseSyncCommand() },
    { "all-changes", ("git diff --merge-base upstream/HEAD") },
    {
        "list-unstaged",
        CliCommand.FromCommandLine("git diff --name-status")
        .AndThen("git ls-files --others --exclude-standard")
    },
    { "list-staged", ("git diff --name-status --staged") },
    {
        "sync-tags",
        CliCommand.FromCommandLine("git show HEAD --pretty=oneline --no-patch --decorate")
        .AndThen("git fetch --tags https://github.com/bonsai-rx/{{REPO_SLUG}}.git")
    },
    { "if-changed", new IfFileChangedCommand() },
    { "if-any-staged", new IfAnyStagedCommand() },
    { "if-exists", new IfFileExistsCommand() },
    { "if-not-exists", new IfFileExistsCommand() { Invert = true } },
    { "push", "git push fork main" },
    { "code", new LaunchVsCodeCommand() },
};

if (args.Length == 0 || args[0] is "--help" or "-?" or "/?" or "/help")
    return Usage(isError: false);

OutputMode outputMode = OutputMode.Default;
bool ignoreExitCode = false;
string? repoGroup = null;
CommandBase? command = null;
ImmutableArray<string>.Builder argumentsBuilder = ImmutableArray.CreateBuilder<string>();

string? changeDirectory = null;

List<string> includePatterns = new();
List<string> excludePatterns = new();

static ReadOnlySpan<char> GetArgValue(ReadOnlySpan<char> argument)
{
    int equalIndex = argument.IndexOf('=');
    if (equalIndex == -1)
        throw new ArgumentException("Argument is malformed", nameof(argument));

    return argument.Slice(equalIndex + 1);
}

void HandleIncludeExcludePattern(List<string> patterns, ReadOnlySpan<char> argument)
{
    argument = GetArgValue(argument);

    foreach (Range patternRange in argument.Split(','))
    {
        ReadOnlySpan<char> pattern = argument[patternRange];
        patterns.Add(pattern.ToString());
    }
}

foreach (string arg in args)
{
    if (command is not null)
    {
        argumentsBuilder.Add(arg);
        continue;
    }

    if (arg == "--long")
    { outputMode = OutputMode.Long; }
    else if (arg == "--short")
    { outputMode = OutputMode.Short; }
    else if (arg == "--shortish")
    { outputMode = OutputMode.Shortish; }
    else if (arg == "--quiet")
    { outputMode = OutputMode.Quiet; }
    else if (arg == "--ignore-exit")
    { ignoreExitCode = true; }
    else if (arg.StartsWith("--include="))
    { HandleIncludeExcludePattern(includePatterns, arg); }
    else if (arg.StartsWith("--exclude="))
    { HandleIncludeExcludePattern(excludePatterns, arg); }
    else if (arg.StartsWith("--cd="))
    { changeDirectory = GetArgValue(arg).ToString(); }
    else if (arg == "--")
    { command ??= runCommand; }
    else if (repoGroup is null)
    { repoGroup = arg; }
    else if (!commands.TryGetValue(arg, out command))
    {
        command = runCommand;
        argumentsBuilder.Add(arg);
    }
}

if (repoGroup is null)
{
    Console.Error.WriteLine("Repo group not specified.");
    Console.Error.WriteLine();
    return Usage(isError: true);
}

if (command is null)
{
    Console.Error.WriteLine("Command not specified.");
    Console.Error.WriteLine();
    return Usage(isError: true);
}

if (!Directory.Exists(repoGroup))
{
    Console.Error.WriteLine($"Repo group '{repoGroup}' does not exist.");
    return 1;
}

if (outputMode == OutputMode.Default)
    outputMode = command.LongOutput ? OutputMode.Long : OutputMode.Short;

ImmutableArray<string> arguments = argumentsBuilder.DrainToImmutable();

const string bar = "=======================================================================================================================";

int successCount = 0;
int failCount = 0;
string oldWorkingDirectory = Environment.CurrentDirectory;

List<(string repoPath, string repoName)> repos = new();
int longestNameLength = 0;

foreach (string repoPath in Directory.EnumerateDirectories(repoGroup, "*", SearchOption.TopDirectoryOnly))
{
    string repoSlug = Path.GetFileName(repoPath);

    if (includePatterns.Count > 0 && includePatterns.All(pattern => !FileSystemName.MatchesSimpleExpression(pattern, repoSlug)))
    {
        Console.WriteLine($"Skipping {repoSlug} (Not included)");
        continue;
    }

    if (excludePatterns.Any(pattern => FileSystemName.MatchesSimpleExpression(pattern, repoSlug)))
    {
        Console.WriteLine($"Skipping {repoSlug} (Excluded)");
        continue;
    }

    if (repoSlug is ".vscode" or ".vs")
        continue;

    repos.Add((repoPath, repoSlug));
    longestNameLength = Math.Max(longestNameLength, repoSlug.Length);
}

int longestNameLengthForShort = Math.Min(10, longestNameLength);

ConsoleColor oldColor = Console.ForegroundColor;
foreach ((string repoPath, string repoSlug) in repos)
{
    try
    {
        string fullRepoPath = Path.GetFullPath(repoPath);
        Environment.CurrentDirectory = repoPath;

        CommandBase? commandToRun = command;
        Context context = new()
        {
            OutputMode = outputMode,
            RepoPath = fullRepoPath,
            RepoSlug = repoSlug,
        };
        ImmutableArray<string> processedArguments = CommandBase.ProcessArguments(context, arguments).ToImmutableArray();

        if (changeDirectory is not null)
            Environment.CurrentDirectory = Path.GetFullPath(CommandBase.ProcessArgument(context, changeDirectory));

        if (!command.ShouldRun(context, processedArguments))
            continue;

        Console.ForegroundColor = ConsoleColor.Green;
        if (outputMode == OutputMode.Long)
        {
            Console.Error.WriteLine(bar);
            Console.Error.WriteLine(repoSlug);
            Console.Error.WriteLine(bar);
        }
        else if (outputMode is OutputMode.Short or OutputMode.Shortish)
        {
            int nameLength = outputMode is OutputMode.Short ? longestNameLengthForShort : longestNameLength;
            string displaySlug = repoSlug;
            if (displaySlug.Length > nameLength)
                displaySlug = displaySlug.Substring(0, nameLength - 1) + '…';
            Console.Error.Write($"{displaySlug.PadLeft(nameLength)}: ");
        }
        Console.ForegroundColor = oldColor;

        do
        {
            commandToRun.Run(context, processedArguments);
            commandToRun = commandToRun.NextCommand;
        } while (commandToRun is not null);
        successCount++;
    }
    catch (ProcessExitException ex) when (!Debugger.IsAttached)
    {
        if (ignoreExitCode)
        {
            successCount++;
            continue;
        }

        failCount++;
        Console.Error.WriteLine(ex.Message);
    }
    catch (Exception ex) when (!Debugger.IsAttached)
    {
        failCount++;
        if (outputMode == OutputMode.Short)
            Console.Error.WriteLine($"{ex.GetType()} {ex.Message}");
        else
            Console.Error.WriteLine(ex);
    }
    finally
    { Environment.CurrentDirectory = oldWorkingDirectory; }
}

Console.WriteLine(bar);
Console.WriteLine($"{successCount}/{successCount + failCount} commands ran successfully");
return failCount;

int Usage(bool isError)
{
    TextWriter writer = isError ? Console.Error : Console.Out;
    writer.WriteLine("Usage:");
    writer.WriteLine("  ForeachRepo [flags] <repo-group> <command> [--] [args...]");
    writer.WriteLine("  ForeachRepo [flags] <repo-group> [run] -- <cli-command> [args...]");
    writer.WriteLine();
    writer.WriteLine("Flags:");
    writer.WriteLine("  --long     Force long output mode");
    writer.WriteLine("  --short    Force short output mode");
    writer.WriteLine("  --quiet    Disable repo headings");
    writer.WriteLine("  --include=<pattern>[,<pattern>] Only include repo names matching glob");
    writer.WriteLine("  --exclude=<pattern>[,<pattern>] Exclude repo names matching glob");
    writer.WriteLine("  --cd=<path> Change directory relative to each repo root before executing command");
    writer.WriteLine();
    writer.WriteLine("Built-in commands:");
    foreach ((string commandName, CommandBase command) in commands)
    {
        writer.WriteLine($"  {commandName}");
    }
    writer.WriteLine();
    writer.WriteLine("Variable substitution:");
    writer.WriteLine("  Arguments passed to command and the --cd parameter can include variable substitutions in the form of {{VARIABLE_NAME}}");
    writer.WriteLine("  The following variables are supported:");
    foreach ((string variable, string description) in Context.EnumerateVariables())
    {
        writer.WriteLine($"  - {variable}  {description}");
    }
    return isError ? 1 : 0;
}
