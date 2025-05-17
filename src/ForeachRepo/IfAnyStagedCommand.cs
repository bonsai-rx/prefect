using System;
using System.Collections.Immutable;

namespace ForeachRepo;

internal sealed class IfAnyStagedCommand : CommandBase
{
    public override void Run(Context context, ImmutableArray<string> args)
    {
        if (args.Length < 1)
            throw new ArgumentException("Not enough arguments. Usage: if-any-staged <command> [args...]", nameof(args));

        string command = args[0];
        args = args[1..];

        if (TryExec("git", "diff", "--name-status", "--staged", "--exit-code") == 1)
            Exec(command, args);
        else
            Console.Error.WriteLine("No staged changes.");
    }
}
