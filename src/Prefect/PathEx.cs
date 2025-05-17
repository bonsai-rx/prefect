using System;
using System.IO;

namespace Prefect;

internal static class PathEx
{
    public static string NormalSlashes(string path)
    {
        if (OperatingSystem.IsWindows())
            path = path.Replace('\\', '/');
        return path;
    }

    public static string GetNormalRelativeTo(string relativeTo, string path)
        => NormalSlashes(Path.GetRelativePath(relativeTo, path));
}
