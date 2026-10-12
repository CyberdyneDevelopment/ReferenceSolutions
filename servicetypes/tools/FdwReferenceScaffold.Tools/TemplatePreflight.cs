using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Spectre.Console;

namespace FdwReferenceScaffold.Tools;

/// <summary>
/// fdw-ref-scaffold and the fdw-ref-api-host template are two separate NuGet installs (a dotnet
/// tool and a template package) -- installing one never installs the other at the package level.
/// But fdw-ref-scaffold's whole job is to shell out to `dotnet new fdw-ref-api-host`, so at runtime
/// it does depend on the template. Rather than just detect that and fail, this installs it.
/// </summary>
internal static class TemplatePreflight
{
    private const string ShortName = "fdw-ref-api-host";
    private const string PackageId = "Reference.Templates";

    /// <summary>
    /// Checked, and installed on demand, once at startup. <c>PrintedSomething</c> tells the
    /// caller whether to pause before the next screen clear -- a silent "was already there" pass
    /// needs no pause, but the install/failure messages do, since AnsiConsole.Clear() also clears
    /// scrollback and would otherwise take them with it before they're readable.
    /// </summary>
    public static async Task<(bool Ready, bool PrintedSomething)> EnsureInstalled()
    {
        if (await IsTemplateInstalled().ConfigureAwait(false))
        {
            return (true, false);
        }

        AnsiConsole.MarkupLine(ReferenceTheme.AccentMarkup($"The {ShortName} template isn't installed yet -- installing {PackageId}..."));

        var startInfo = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("new");
        startInfo.ArgumentList.Add("install");
        startInfo.ArgumentList.Add(PackageId);

        using var install = Process.Start(startInfo);
        if (install is null)
        {
            return (false, true);
        }

        await install.WaitForExitAsync().ConfigureAwait(false);

        if (install.ExitCode != 0)
        {
            AnsiConsole.MarkupLine(ReferenceTheme.DangerMarkup(
                $"Installing {PackageId} failed. Check that a NuGet source carrying it (e.g. rayburn-packages-premain) is configured, then run 'dotnet new install {PackageId}' by hand."));
            return (false, true);
        }

        return (await IsTemplateInstalled().ConfigureAwait(false), true);
    }

    private static async Task<bool> IsTemplateInstalled()
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("new");
        startInfo.ArgumentList.Add("list");
        startInfo.ArgumentList.Add(ShortName);

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return false;
        }

        var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        await process.WaitForExitAsync().ConfigureAwait(false);

        return output.Contains(ShortName, StringComparison.Ordinal);
    }
}
