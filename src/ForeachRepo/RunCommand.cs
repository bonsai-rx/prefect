using System;
using System.Collections.Immutable;

namespace ForeachRepo;

internal sealed class RunCommand : CommandBase
{
    public override void Run(Context context, ImmutableArray<string> args)
    {
        if (args.Length < 1)
            throw new ArgumentException("No command specified.");

        string command = args[0];
        args = args[1..];

        Exec(command, args);
    }
}
