#define INTERACTIVE
#define BONSAI_MODE
using Prefect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;
Console.Clear();
Console.WriteLine("Loading ruleset...");
#if BONSAI_MODE
Ruleset rules = new(@"C:\Projects\NeuroGEARS\CommonCiProject\prefect\reference\");
#else
Ruleset rules = new(@"C:\Projects\NeuroGEARS\CommonCiProject\prefect\reference-harp\");
#endif

#if !true
{
    Console.WriteLine("Rules to be enforced:");
    foreach (Rule rule in rules)
    {
        Console.WriteLine($"* {rule.Description}");
    }
}
#endif


Console.WriteLine();

bool enableAutomaticFixes = true;

#if BONSAI_MODE
var repos = Directory.EnumerateDirectories(@"C:\Projects\NeuroGEARS\CommonCiProject\bonsai-rx-all\", "*", SearchOption.TopDirectoryOnly);
#else
var repos = Directory.EnumerateDirectories(@"C:\Projects\NeuroGEARS\CommonCiProject\harp-all\", "*", SearchOption.TopDirectoryOnly);
#endif

bool isAutomaticRerun = false;
foreach (string repoPath in repos)
{
    if (Path.GetFileName(repoPath) is ".vscode" or ".vs")
        continue;

#if INTERACTIVE
    Again:
#endif
    Repo repo = new(repoPath, rules.Kind);

    ConsoleColor oldColor = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write($"Validating '{Path.GetFileName(repoPath)}' ({repo.ProjectName})...");

    bool noFailures = true;
    bool hadFixableErrors = false;
    bool hadUnfixableErrors = false;
    foreach (Rule rule in rules)
    {
        if (ShouldSkip(rule, repo))
            continue;

        if (rule.Validate(repo) is string failReason)
        {
            if (noFailures)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("FAIL");
                Console.ForegroundColor = oldColor;
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

    if (noFailures)
    {
        isAutomaticRerun = false;
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine("PASS");
        Console.ForegroundColor = oldColor;
    }
#if !INTERACTIVE
    else
    { Console.WriteLine(); }
#else
    else if (!hadUnfixableErrors && !isAutomaticRerun)
    {
        Debug.Assert(hadFixableErrors);
        Console.WriteLine("All problems were automatically fixed.");
        isAutomaticRerun = true;
        goto Again;
    }
    else
    {
        Console.WriteLine();

        if (isAutomaticRerun)
            Console.WriteLine("Auto-fixing seems to be stuck :/");

        isAutomaticRerun = false;
        Console.WriteLine("Fix issues and press enter to continue...");
        while (true)
        {
            ConsoleKey key = Console.ReadKey(intercept: true).Key;
            if (key == ConsoleKey.Enter)
            {
                Console.Clear();
                goto Again;
            }
            else if (key == ConsoleKey.Escape)
            {
                return;
            }
        }
    }
#endif
}

bool ShouldSkip(Rule rule, Repo repo)
{
    // These packages are BSD2 and GPLv3 respectively
    if (repo.RepoSlug is "tld" or "cmt" && rule is MitLicenseRule)
        return true;

    return false;
}
