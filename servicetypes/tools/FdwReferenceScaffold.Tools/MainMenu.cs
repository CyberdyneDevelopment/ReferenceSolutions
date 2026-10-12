using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FdwReferenceScaffold.Tools.Steps;
using Spectre.Console;

namespace FdwReferenceScaffold.Tools;

/// <summary>
/// The wizard's home screen: a looping menu over each fdw-ref-api-host decision group, plus
/// Review &amp; Generate. Re-entering a step keeps whatever was already answered as its default.
/// </summary>
internal static class MainMenu
{
    private const string ProjectItem = "Project (name, output directory)";
    private const string ConnectionItem = "Platform Configuration connection (ConfigurationDb, authentication, secrets)";
    private const string OtherConnectionsItem = "Other connections (additional DataStores)";
    private const string HostItem = "Host configuration (secret manager, ServerConfiguration root)";
    private const string ConfigurationItem = "Current configuration (walk the full tree)";
    private const string ReviewItem = "Review and generate";
    private const string HelpItem = "Help";
    private const string QuitItem = "Quit without generating";

    public static async Task<int> Run()
    {
        var answers = new WizardAnswers();
        var visited = new HashSet<string>(StringComparer.Ordinal);

        // Checked (and installed if missing) once, right at startup rather than only when you
        // hit Generate -- dotnet tool install and dotnet new install are separate SDK subsystems
        // with no hook between them, so this is as close to "one install gets you ready to go" as
        // is actually achievable. A failure here doesn't block the menu (you can still walk the
        // configuration offline); Review & Generate reports it again using this cached result
        // instead of retrying a doomed install.
        var (templateReady, printed) = await TemplatePreflight.EnsureInstalled().ConfigureAwait(false);
        if (printed)
        {
            AnsiConsole.WriteLine();
            PauseForEnter();
        }

        while (true)
        {
            RenderHeader(answers);

            var items = new List<string>
            {
                Mark(ProjectItem, visited),
                Mark(ConnectionItem, visited),
                Mark(OtherConnectionsItem, visited),
                Mark(HostItem, visited),
                ConfigurationItem,
                ReviewItem,
                HelpItem,
                QuitItem,
            };

            var prompt = new SelectionPrompt<string>()
                .Title(ReferenceTheme.Markup("What do you want to configure?"))
                .HighlightStyle(ReferenceTheme.Selected)
                .PageSize(14)
                .AddChoices(items);

            var choice = Strip(AnsiConsole.Prompt(prompt));
            AnsiConsole.WriteLine();

            switch (choice)
            {
                case ProjectItem:
                    ProjectStep.Run(answers);
                    visited.Add(ProjectItem);
                    break;
                case ConnectionItem:
                    ConnectionStep.Run(answers);
                    visited.Add(ConnectionItem);
                    break;
                case OtherConnectionsItem:
                    OtherConnectionsStep.Run(answers);
                    visited.Add(OtherConnectionsItem);
                    break;
                case HostItem:
                    HostStep.Run(answers);
                    visited.Add(HostItem);
                    break;
                case ConfigurationItem:
                    AnsiConsole.Write(ConfigurationTree.Build(answers));
                    AnsiConsole.WriteLine();
                    PauseForEnter();
                    break;
                case ReviewItem:
                    var exitCode = await ReviewAndMaybeGenerate(answers, templateReady).ConfigureAwait(false);
                    if (exitCode.HasValue)
                    {
                        return exitCode.Value;
                    }

                    break;
                case HelpItem:
                    HelpText.Show();
                    break;
                case QuitItem:
                    return 0;
            }

            AnsiConsole.WriteLine();
        }
    }

    private static async Task<int?> ReviewAndMaybeGenerate(WizardAnswers answers, bool templateReady)
    {
        if (string.IsNullOrWhiteSpace(answers.Name))
        {
            AnsiConsole.MarkupLine(ReferenceTheme.DangerMarkup($"Set a project name under '{ProjectItem}' first."));
            PauseForEnter();
            return null;
        }

        if (!templateReady)
        {
            AnsiConsole.MarkupLine(ReferenceTheme.DangerMarkup("The fdw-ref-api-host template still isn't installed -- see the message at startup for why, fix it, then restart fdw-ref-scaffold."));
            PauseForEnter();
            return null;
        }

        ReviewStep.Render(answers);

        if (!Prompts.Confirm("Generate now?", true))
        {
            return null;
        }

        var args = TemplateArgumentBuilder.Build(answers);
        AnsiConsole.WriteLine();
        var exitCode = await TemplateRunner.Run(args).ConfigureAwait(false);

        if (exitCode == 0)
        {
            AnsiConsole.MarkupLine(ReferenceTheme.AccentMarkup("Generated successfully."));
            AnsiConsole.WriteLine();
            await PostGenerationIntrospection.Run(answers).ConfigureAwait(false);
        }
        else
        {
            AnsiConsole.MarkupLine(ReferenceTheme.DangerMarkup($"dotnet new exited with code {exitCode}."));
        }

        return exitCode;
    }

    private const string DoneSuffix = " ✓";

    private static string Mark(string item, ICollection<string> visited) =>
        visited.Contains(item) ? item + DoneSuffix : item;

    private static string Strip(string item) =>
        item.EndsWith(DoneSuffix, StringComparison.Ordinal) ? item[..^DoneSuffix.Length] : item;

    private static void RenderHeader(WizardAnswers answers)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("reference") { Color = ReferenceTheme.Brand });
        AnsiConsole.MarkupLine(ReferenceTheme.AccentMarkup("scaffold — interactive wizard for `dotnet new fdw-ref-api-host`."));
        AnsiConsole.WriteLine();
        AnsiConsole.Write(StatusPanel.Build(answers));
        AnsiConsole.WriteLine();
    }

    private static void PauseForEnter()
    {
        AnsiConsole.MarkupLine("[grey]Press Enter to continue...[/]");
        Console.ReadLine();
    }
}
