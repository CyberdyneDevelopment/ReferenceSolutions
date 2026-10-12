using System.Linq;
using Spectre.Console;

namespace FdwReferenceScaffold.Tools.Steps;

/// <summary>Renders the effective answers and the resulting dotnet new command line before generation.</summary>
internal static class ReviewStep
{
    public static void Render(WizardAnswers answers)
    {
        AnsiConsole.Write(ReferenceTheme.Rule("Review"));

        var table = new Table().Border(TableBorder.Rounded).BorderColor(ReferenceTheme.Border);
        table.AddColumn(new TableColumn(ReferenceTheme.Markup("Setting")));
        table.AddColumn(new TableColumn(ReferenceTheme.AccentMarkup("Value")));

        table.AddRow("Name", answers.Name);
        table.AddRow("Output directory", string.IsNullOrWhiteSpace(answers.OutputDirectory) ? "(default)" : answers.OutputDirectory);
        table.AddRow("Platform Configuration type", answers.ConnectionType);

        if (answers.IsServerBacked)
        {
            table.AddRow("Platform Configuration server", answers.DbServer);
            table.AddRow("Platform Configuration database", answers.DbName);
            table.AddRow("Platform Configuration authentication", answers.AuthenticationType);

            if (answers.NeedsConnectionSecret)
            {
                table.AddRow("Secret key", answers.SecretKeyName);
            }
        }
        else
        {
            table.AddRow("Platform Configuration path", answers.ConfigPath);
        }

        table.AddRow("Other connections", answers.OtherConnections.Count == 0
            ? "none"
            : string.Join(", ", answers.OtherConnections.Select(c => $"{c.Name} ({c.ConnectionType})")));

        table.AddRow("Secret manager", answers.SecretManager);
        table.AddRow("ServerConfiguration root", answers.ServerConfigRoot);

        AnsiConsole.Write(table);

        var args = TemplateArgumentBuilder.Build(answers);
        AnsiConsole.MarkupLine(ReferenceTheme.Markup("Command:"));
        AnsiConsole.WriteLine("dotnet " + string.Join(' ', args.Select(QuoteIfNeeded)));
    }

    private static string QuoteIfNeeded(string arg)
    {
        return arg.Contains(' ', System.StringComparison.Ordinal) ? $"\"{arg}\"" : arg;
    }
}
