using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ForeachRepo;

internal abstract partial class CommandBase
{
    public bool LongOutput { get; set; } = true;

    public CommandBase? NextCommand { get; private set; }

    public virtual bool ShouldRun(Context context, ImmutableArray<string> args)
        => true;

    public abstract void Run(Context context, ImmutableArray<string> args);

    public CommandBase AndThen(CommandBase other)
    {
        CommandBase parent = this;

        while (parent.NextCommand is not null)
            parent = parent.NextCommand;

        parent.NextCommand = other;
        return this;
    }

    protected bool __UseVsCodeEnvironmentVars = false;

    protected Process StartProcess(string command, params IEnumerable<string> args)
    {
        Process process = new();
        process.StartInfo = new(command, args)
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
        };

        process.StartInfo.EnvironmentVariables.Add("GIT_CONFIG_COUNT", "1");
        process.StartInfo.EnvironmentVariables.Add("GIT_CONFIG_KEY_0", "color.ui");
        process.StartInfo.EnvironmentVariables.Add("GIT_CONFIG_VALUE_0", "always");

        if (__UseVsCodeEnvironmentVars)
        {
            process.StartInfo.EnvironmentVariables.Remove("VSCODE_DEV");
            process.StartInfo.EnvironmentVariables.Add("ELECTRON_RUN_AS_NODE", "1");
        }

        static void WriteOutput(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is null)
                return;

            Console.WriteLine(e.Data);
        }

        process.OutputDataReceived += WriteOutput;
        process.ErrorDataReceived += WriteOutput;
        process.EnableRaisingEvents = true;
        bool started;

        const int FILE_NOT_FOUND = 2;
        try
        { started = process.Start(); }
        catch (Win32Exception ex) when (ex.NativeErrorCode == FILE_NOT_FOUND)
        { throw new ProcessExitException(process, "Command not found"); }

        if (!started)
            throw new InvalidOperationException("Process did not start."); // Unclear when this ever actually happens

        process.StandardInput.Close();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return process;
    }

    protected void Exec(string command, params IEnumerable<string> args)
    {
        using Process process = StartProcess(command, args);
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new ProcessExitException(process);
    }

    protected int TryExec(string command, params IEnumerable<string> args)
    {
        using Process process = StartProcess(command, args);
        process.WaitForExit();
        return process.ExitCode;
    }

    public static implicit operator CommandBase(string commandLine)
        => CliCommand.FromCommandLine(commandLine);

    public static implicit operator CommandBase(string[] commandLineParts)
        => new CliCommand(commandLineParts);

    public static implicit operator CommandBase((string commandLine, bool longOutput) commandDefinition)
    {
        CommandBase result = commandDefinition.commandLine;
        result.LongOutput = commandDefinition.longOutput;
        return result;
    }

    public static implicit operator CommandBase((string[] commandLineParts, bool longOutput) commandDefinition)
    {
        CommandBase result = commandDefinition.commandLineParts;
        result.LongOutput = commandDefinition.longOutput;
        return result;
    }

    [GeneratedRegex(@"\{\{(?<variable>[A-Za-z0-9_]+)\}\}")]
    private static partial Regex VariableRegex();

    public static string ProcessArgument(Context context, string argument)
        => VariableRegex().Replace(argument, m => context.GetVariable(m.Groups["variable"].ValueSpan));

    public static IEnumerable<string> ProcessArguments(Context context, IEnumerable<string> arguments)
    {
        foreach (string argument in arguments)
            yield return ProcessArgument(context, argument);
    }
}
