using System.Collections.Immutable;

namespace ForeachRepo;

internal sealed class WellKnownCommand : CommandBase
{
    public readonly string Command;

    public WellKnownCommand(string command)
        => Command = command;

    public override void Run(Context context, ImmutableArray<string> args)
        => Exec(Command, args);
}
