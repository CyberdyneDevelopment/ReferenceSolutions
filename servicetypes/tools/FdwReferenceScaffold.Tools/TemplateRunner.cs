using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FdwReferenceScaffold.Tools;

/// <summary>
/// Shells out to the installed <c>dotnet new</c> CLI with the wizard's chosen arguments,
/// streaming its own stdout/stderr straight through rather than re-parsing them.
/// </summary>
internal static class TemplateRunner
{
    public static async Task<int> Run(IReadOnlyList<string> args)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return 1;
        }

        await process.WaitForExitAsync().ConfigureAwait(false);
        return process.ExitCode;
    }
}
