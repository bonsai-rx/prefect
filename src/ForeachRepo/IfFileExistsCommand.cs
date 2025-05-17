using System;
using System.Collections.Immutable;
using System.IO;

namespace ForeachRepo;

internal sealed class IfFileExistsCommand : CommandBase
{
    public bool Invert { get; init; }

    private bool ShouldRun(string file)
    {
        bool exists = File.Exists(file) || Directory.Exists(file);
        return Invert ? !exists : exists;
    }

    public override bool ShouldRun(Context context, ImmutableArray<string> args)
    {
        if (args.Length < 1)
            return true;

        return ShouldRun(args[0]);
    }

    public override void Run(Context context, ImmutableArray<string> args)
    {
        if (args.Length < 2)
            throw new ArgumentException($"Not enough arguments. Usage: if{(Invert ? "-not" : "")}-exists <file-path> <command> [args...]", nameof(args));

        string file = args[0];
        string command = args[1];
        args = args[2..];

        if (ShouldRun(file))
            Exec(command, args);
        else
            Console.Error.WriteLine($"'{file}' not found.");
    }
}
