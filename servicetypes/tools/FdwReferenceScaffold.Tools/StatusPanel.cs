using System.Linq;
using Spectre.Console;

namespace FdwReferenceScaffold.Tools;

/// <summary>
/// A compact, always-visible summary redrawn above the menu every time it loops back — so what's
/// been set up so far is visible without opening anything, the closest terminal equivalent to a
/// persistent top menu bar Spectre.Console (no mouse, no floating overlays) can actually render.
/// </summary>
internal static class StatusPanel
{
    public static Panel Build(WizardAnswers answers)
    {
        var grid = new Grid();
        grid.AddColumn(new GridColumn().NoWrap().PadRight(2));
        grid.AddColumn();

        AddRow(grid, "Project", string.IsNullOrWhiteSpace(answers.Name) ? "(not set)" : answers.Name);
        AddRow(grid, "Platform Configuration", answers.IsServerBacked ? $"{answers.ConnectionType} @ {answers.DbServer}" : $"{answers.ConnectionType} ({answers.ConfigPath})");
        AddRow(grid, "Other connections", answers.OtherConnections.Count == 0 ? "none" : string.Join(", ", answers.OtherConnections.Select(static c => c.Name)));
        AddRow(grid, "Secret manager", answers.SecretManager);
        AddRow(grid, "ServerConfiguration root", answers.ServerConfigRoot);

        return new Panel(grid)
        {
            Header = new PanelHeader(ReferenceTheme.Markup("Current configuration")),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(ReferenceTheme.Border),
            Expand = true,
        };
    }

    private static void AddRow(Grid grid, string label, string value) =>
        grid.AddRow($"[grey]{label}[/]", ReferenceTheme.AccentMarkup(value));
}
