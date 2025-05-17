using System;
using System.Collections.Immutable;

namespace ForeachRepo;

internal sealed class IfFileChangedCommand : CommandBase
{
    public override void Run(Context context, ImmutableArray<string> args)
    {
        if (args.Length < 2)
            throw new ArgumentException("Not enough arguments. Usage: if-changed <file-path> <command> [args...]", nameof(args));

        string file = args[0];
        string command = args[1];
        args = args[2..];

        if (TryExec("git", "diff", "--name-status", "--exit-code", file) == 1)
            Exec(command, args);
        else
            Console.Error.WriteLine($"'{file}' has not changed.");
    }
}
