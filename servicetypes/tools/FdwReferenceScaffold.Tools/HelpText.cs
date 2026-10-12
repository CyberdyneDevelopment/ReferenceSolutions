using System.IO;
using System.Reflection;
using Spectre.Console;

namespace FdwReferenceScaffold.Tools;

/// <summary>Prints the embedded README.md — kept as the single source of truth for help text.</summary>
internal static class HelpText
{
    private const string ResourceName = "FdwReferenceScaffold.Tools.README.md";

    public static void Show()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(ResourceName);
        if (stream is null)
        {
            AnsiConsole.MarkupLine(ReferenceTheme.DangerMarkup("README.md wasn't embedded in this build."));
            return;
        }

        using var reader = new StreamReader(stream);
        // Written with AnsiConsole.Write(new Text(...)), not MarkupLine — README.md's own
        // backticks and asterisks are not Spectre markup and must not be parsed as any.
        AnsiConsole.Write(new Text(reader.ReadToEnd()));
    }
}
