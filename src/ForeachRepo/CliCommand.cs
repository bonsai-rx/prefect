using System;
using System.Collections.Immutable;
using System.Linq;

namespace ForeachRepo;

internal sealed class CliCommand : CommandBase
{
    public readonly string Command;
    public readonly ImmutableArray<string> Arguments;

    public CliCommand(string command, params ImmutableArray<string> arguments)
    {
        Command = command;
        Arguments = arguments;
    }

    public CliCommand(ReadOnlySpan<string> commandLineParts)
    {
        if (commandLineParts.Length < 1)
            throw new ArgumentException("Must pass at least one command line part.", nameof(commandLineParts));

        Command = commandLineParts[0];
        Arguments = commandLineParts.Slice(1).ToImmutableArray();
    }

    public static CliCommand FromCommandLine(string commandLine)
        => new CliCommand(commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    public override void Run(Context context, ImmutableArray<string> args)
        => Exec(Command, ProcessArguments(context, Arguments).Concat(args));
}
