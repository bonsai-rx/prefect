using System;
using System.Collections.Immutable;
using System.IO;

namespace ForeachRepo;

internal sealed class LaunchVsCodeCommand : CommandBase
{
    public override void Run(Context context, ImmutableArray<string> args)
    {
        string vsCodeInstallPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Microsoft VS Code");
        __UseVsCodeEnvironmentVars = true;
        Exec
        (
            Path.Combine(vsCodeInstallPath, "Code.exe"),
            args.Insert(0, Path.Combine(vsCodeInstallPath, @"resources\app\out\cli.js"))
        );
    }
}
