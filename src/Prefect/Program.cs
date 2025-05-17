#define MAKE_TABLE
#define INTERACTIVE
using Prefect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;
Console.Clear();
Console.WriteLine("Loading ruleset...");
//Ruleset rules = new(@"C:\Projects\NeuroGEARS\CommonCiProject\prefect\reference\");
Ruleset rules = new(@"C:\Projects\NeuroGEARS\CommonCiProject\prefect\reference-harp\");

#if !true
{
    Console.WriteLine("Rules to be enforced:");
    foreach (Rule rule in rules)
    {
        Console.WriteLine($"* {rule.Description}");
    }
}
#endif

#if false
using (StreamWriter f = new("definitely-correct.txt"))
{
    foreach (Rule rule in rules)
    {
        if (rule is FileExistsRule { AllowFixupOverwrite: true } fileRule && !fileRule.RelativePath.Contains('$'))
            f.WriteLine(fileRule.RelativePath);
    }
}
#endif

Console.WriteLine();

bool enableAutomaticFixes = !true;

//var repos = Directory.EnumerateDirectories(@"C:\Projects\NeuroGEARS\CommonCiProject\bonsai-rx-all\", "*", SearchOption.TopDirectoryOnly);
var repos = Directory.EnumerateDirectories(@"C:\Projects\NeuroGEARS\CommonCiProject\harp-all\", "*", SearchOption.TopDirectoryOnly);

#if MAKE_TABLE
//using StreamWriter f = new(@"C:\Projects\NeuroGEARS\NgrDavid.github\profile\README.md", append: true);
using StreamWriter f = new(@"Table.md", append: false);
int col = 0;
f.WriteLine();
f.WriteLine();
f.WriteLine("| | |");
f.WriteLine("|-----|-----|");
#endif

Dictionary<string, int> prLookups = new() {
    { "device.analoginput", 12 },
    { "device.audioswitch", 9 },
    { "device.behavior", 26 },
    { "device.cameracontroller", 6 },
    { "device.cameracontrollergen2", 7 },
    { "device.clocksynchronizer", 5 },
    { "device.faststepper", 2 },
    { "device.inputexpander", 8 },
    { "device.ledarray", 12 },
    { "device.loadcells", 7 },
    { "device.olfactometer", 26 },
    { "device.outputexpander", 17 },
    { "device.rfidreader", 9 },
    { "device.rgbarray", 7 },
    { "device.soundcard", 24 },
    { "device.stepperdriver", 35 },
    { "device.synchronizer", 11 },
    { "device.syringepump", 13 },
    { "device.timestampgeneratorgen3", 12 },
    { "device.vestibularH1", 11 },
    { "device.vestibularH2", 17 },
};

bool isAutomaticRerun = false;
foreach (string repoPath in repos)
{
    if (Path.GetFileName(repoPath) is ".vscode" or ".vs")
        continue;

#if INTERACTIVE
    Again:
#endif
    Repo repo = new(repoPath, rules.Kind);

#if MAKE_TABLE
    f.Write(String.Join(" ", [
        $"[![{repo.ProjectName}](https://img.shields.io/badge/--blue?logo=nuget)](https://dev.nugettest.org/packages/{repo.ProjectName})",
        //$"[![{repo.ProjectName}](https://img.shields.io/badge/--1f2328?logo=github)](https://ngrdavid.github.io/{repo.RepoSlug})",
        $"[![](https://img.shields.io/github/pulls/detail/state/harp-tech/{repo.RepoSlug}/{prLookups[repo.RepoSlug]}?label=PR)](https://github.com/harp-tech/{repo.RepoSlug}/{prLookups[repo.RepoSlug]})",
        $"[![](https://github.com/NgrDavid/{repo.RepoSlug}/actions/workflows/{repo.ProjectName}.yml/badge.svg)](https://github.com/NgrDavid/{repo.RepoSlug}/actions/workflows/{repo.ProjectName}.yml)",
    ]));
    col++;
    if (col == 2)
    {
        f.WriteLine();
        col = 0;
    }
    else
    { f.Write(" | "); }
    continue;
#endif

    ConsoleColor oldColor = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write($"Validating '{Path.GetFileName(repoPath)}' ({repo.ProjectName})...");

    bool noFailures = true;
    bool hadFixableErrors = false;
    bool hadUnfixableErrors = false;
    foreach (Rule rule in rules)
    {
        //if (rule is not CSharpProjectValidationRule)
        //    continue;

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
    { console.WriteLine(); }
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
#endif
    }
}

bool ShouldSkip(Rule rule, Repo repo)
{
    // These packages are BSD2 and GPLv3 respectively
    if (repo.RepoSlug is "tld" or "cmt" && rule is BonsaiFoundationLicenseRule)
        return true;

    return false;
}
