using System.Collections.Generic;
using System.Linq;
using Spectre.Console;

namespace FdwReferenceScaffold.Tools;

/// <summary>
/// Rayburn-themed wrappers around the three Spectre.Console prompt kinds the wizard needs:
/// free text, yes/no, and a described multiple-choice list.
/// </summary>
internal static class Prompts
{
    public static string Text(string title, string defaultValue)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>(ReferenceTheme.Markup(title))
                .DefaultValue(defaultValue)
                .PromptStyle(ReferenceTheme.Accent));
    }

    public static bool Confirm(string title, bool defaultValue)
    {
        return AnsiConsole.Prompt(
            new ConfirmationPrompt(ReferenceTheme.Markup(title)) { DefaultValue = defaultValue });
    }

    /// <summary>
    /// Presents <paramref name="choices"/> (value, description) as a selection list, each rendered
    /// "value — description", and returns the chosen value.
    /// </summary>
    public static string Choice(string title, IReadOnlyList<(string Value, string Description)> choices, string defaultValue)
    {
        var display = choices.Select(c => $"{c.Value} — {c.Description}").ToList();
        var byDisplay = choices.ToDictionary(c => $"{c.Value} — {c.Description}", c => c.Value);

        var defaultDisplay = display.FirstOrDefault(d => byDisplay[d] == defaultValue);
        if (defaultDisplay is not null)
        {
            display.Remove(defaultDisplay);
            display.Insert(0, defaultDisplay);
        }

        var prompt = new SelectionPrompt<string>()
            .Title(ReferenceTheme.Markup(title))
            .HighlightStyle(ReferenceTheme.Selected)
            .PageSize(10)
            .AddChoices(display);

        var selected = AnsiConsole.Prompt(prompt);
        return byDisplay[selected];
    }
}
