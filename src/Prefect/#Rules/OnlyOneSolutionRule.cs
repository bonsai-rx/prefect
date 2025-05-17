using System;
using System.IO;
using System.Linq;

namespace Prefect;

internal sealed class OnlyOneSolutionRule : Rule
{
    public override string Description => "Only one solution should be present in the root.";

    public override string? Validate(Repo repo)
    {
        if (repo.EnumerateFiles("*.sln", SearchOption.TopDirectoryOnly).Count() > 1)
            return "Multiple solution files are present in the root.";

        return null;
    }
}
