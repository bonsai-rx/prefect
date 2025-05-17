using System;
using System.Diagnostics;

namespace ForeachRepo;

internal sealed class ProcessExitException : Exception
{
    public ProcessExitException(Process process, string failReason)
        : base(MakeMessage(process, failReason))
    { }

    public ProcessExitException(Process process)
        : this(process, $"exited with code {process.ExitCode}")
    { }

    private static string MakeMessage(Process process, string failReason)
    {
        string commandLine = process.StartInfo.FileName;

        if (process.StartInfo.Arguments.Length > 0)
        {
            Debug.Assert(process.StartInfo.ArgumentList.Count == 0);
            commandLine += $" {process.StartInfo.Arguments}";
        }
        else
        {
            foreach (string argument in process.StartInfo.ArgumentList)
            {
                commandLine += $" {argument}";
            }
        }

        return $"`{commandLine}` {failReason}";
    }
}
