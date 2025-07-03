using Prefect;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.Diagnostics;
using System.IO;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

if (Debugger.IsAttached)
    Console.Clear();

//-------------------------------------------------------------------------------------------------
// Command line parsing
//-------------------------------------------------------------------------------------------------
Argument<DirectoryInfo> templatePathArgument = ArgumentValidation.AcceptExistingOnly(
    new Argument<DirectoryInfo>("reference-template")
{
    Description = "Required path to the reference template to use for validation. (See detailed description below.)",
    Arity = ArgumentArity.ExactlyOne,
});

Argument<List<DirectoryInfo>> repoArguments = ArgumentValidation.AcceptExistingOnly(
    new Argument<List<DirectoryInfo>>("repo")
{
    Description = "The repository or set of repositories to validate. Multiple can be specified. If the specified " +
                  "path is a Git repository, then that single repository will be added to the validation set. If " +
                  "the specified path is a non-Git directory, then each Git repository directly under it is added " +
                  "to the validation set. Otherwise, if no repositories are specified, the list of configured " +
                  "validation rules will be printed.",
    Arity = ArgumentArity.ZeroOrMore
});

Option<string> projectNameOption = new("--project-name")
{
    Description = "Overrides the project name instead of using automatic detection, cannot be used with sets of " +
                  "multiple repositories. This flag is most useful when provisioning new repositories where the " +
                  "automatic name detection has nothing to work with."
};

Option<bool> interactiveOption = new("--interactive", "-i")
{
    Description = "Enables interactive mode. In interactive mode Prefect will pause and wait for the user to correct " +
                  "issues whenever a repository fails validation, after which the repository will be checked again."
};

Option<bool> automaticFixesOption = new("--auto-fix")
{
    Description = "Attempt to automatically fix certain rule violations. (Note fixes may be destructive.)"
};

RootCommand rootCommand = new("Validate a set of repositories against a Prefect reference template.");
rootCommand.Arguments.Add(templatePathArgument);
rootCommand.Arguments.Add(repoArguments);
rootCommand.Options.Add(projectNameOption);
rootCommand.Options.Add(interactiveOption);
rootCommand.Options.Add(automaticFixesOption);
for (int i = 0; i < rootCommand.Options.Count; i++)
{
    if (rootCommand.Options[i] is HelpOption defaultHelpOption)
    {
        defaultHelpOption.Action = new ExtendedHelpAction((HelpAction)defaultHelpOption.Action!);
        break;
    }
}

//-------------------------------------------------------------------------------------------------
// Execution
//-------------------------------------------------------------------------------------------------
rootCommand.SetAction(parseResult =>
{
    var templateDirectory = parseResult.GetRequiredValue(templatePathArgument);
    var repositories = parseResult.GetValue(repoArguments);
    var projectNameOverride = parseResult.GetValue(projectNameOption);
    var enableAutomaticFixes = parseResult.GetValue(automaticFixesOption);
    var interactiveMode = parseResult.GetValue(interactiveOption);

    // Load ruleset
    Ruleset rules = new(templateDirectory.FullName);

    // Print ruleset when there's no repos to validate
    if (repositories is null || repositories.Count == 0)
    {
        Console.WriteLine($"The following rules will be enforced by '{templateDirectory}':");
        foreach (Rule rule in rules)
            Console.WriteLine($"* {rule.Description}");

        return 0;
    }

    // Enumerate repositories to validate
    List<string> validationSet = new();
    {
        bool haveErrors = false;

        foreach (DirectoryInfo repoDirectory in repositories)
        {
            var repoArgument = repoDirectory.FullName;

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
});

var parseResult = rootCommand.Parse(args);
return parseResult.Invoke();
