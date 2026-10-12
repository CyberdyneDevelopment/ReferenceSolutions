using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;

namespace FdwReferenceScaffold.Tools.Steps;

/// <summary>
/// Connections beyond PlatformConfiguration — extra DataStores the app will read from. The
/// template has no symbol for a variable-length list, so these are patched into the generated
/// configurationSchema.json after `dotnet new` runs (see ConfigurationSchemaPatcher).
/// </summary>
internal static class OtherConnectionsStep
{
    private static readonly IReadOnlySet<string> ReservedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "PlatformConfiguration",
        "ServerConfiguration",
    };

    private static readonly IReadOnlyList<(string Value, string Description)> ConnectionTypes =
    [
        ("MsSql", "SQL Server."),
        ("PostgreSql", "PostgreSQL."),
        ("Sqlite", "A SQLite file."),
        ("FileSystem", "Configuration held as files on disk."),
    ];

    public static void Run(WizardAnswers answers)
    {
        AnsiConsole.Write(ReferenceTheme.Rule("Other connections"));

        while (true)
        {
            if (answers.OtherConnections.Count > 0)
            {
                var table = new Table().Border(TableBorder.Rounded).BorderColor(ReferenceTheme.Border);
                table.AddColumn("Name");
                table.AddColumn("Type");
                foreach (var connection in answers.OtherConnections)
                {
                    table.AddRow(connection.Name, connection.ConnectionType);
                }

                AnsiConsole.Write(table);
            }
            else
            {
                AnsiConsole.MarkupLine(ReferenceTheme.AccentMarkup("No other connections yet — PlatformConfiguration and ServerConfiguration are always present."));
            }

            const string AddItem = "Add a connection";
            const string RemoveItem = "Remove a connection";
            const string DoneItem = "Done";

            var choices = new List<string> { AddItem };
            if (answers.OtherConnections.Count > 0)
            {
                choices.Add(RemoveItem);
            }

            choices.Add(DoneItem);

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title(ReferenceTheme.Markup("Other connections"))
                    .HighlightStyle(ReferenceTheme.Selected)
                    .AddChoices(choices));

            if (choice == DoneItem)
            {
                return;
            }

            if (choice == AddItem)
            {
                Add(answers);
            }
            else
            {
                Remove(answers);
            }

            AnsiConsole.WriteLine();
        }
    }

    private static void Add(WizardAnswers answers)
    {
        string name;
        while (true)
        {
            name = Prompts.Text("Connection name", string.Empty);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (ReservedNames.Contains(name))
            {
                AnsiConsole.MarkupLine(ReferenceTheme.DangerMarkup($"'{name}' is reserved."));
                continue;
            }

            if (answers.OtherConnections.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                AnsiConsole.MarkupLine(ReferenceTheme.DangerMarkup($"'{name}' is already used."));
                continue;
            }

            break;
        }

        var connection = new ConnectionAnswer { Name = name };
        connection.ConnectionType = Prompts.Choice("Connection type", ConnectionTypes, connection.ConnectionType);

        if (connection.IsServerBacked)
        {
            connection.DbServer = Prompts.Text("Server host", connection.DbServer);
            connection.DbName = Prompts.Text("Database name", connection.DbName);
            connection.DbPort = ConnectionDefaults.PromptPort(connection.ConnectionType, connection.DbPort);
            connection.DbUser = Prompts.Text("Database login", connection.DbUser);
            connection.AuthenticationType = ConnectionStep.AuthenticationOptions(connection.ConnectionType)
                .Any(option => option.Value == connection.AuthenticationType)
                ? connection.AuthenticationType
                : ConnectionStep.AuthenticationOptions(connection.ConnectionType)[0].Value;
            connection.AuthenticationType = Prompts.Choice(
                "Authentication type",
                ConnectionStep.AuthenticationOptions(connection.ConnectionType),
                connection.AuthenticationType);

            if (connection.NeedsSecret)
            {
                connection.SecretKeyName = Prompts.Text(
                    "Logical name of the connection password (read as FDW_SECRET_{this} from Platform Configuration's secret manager)",
                    string.IsNullOrWhiteSpace(connection.SecretKeyName) ? $"{name.ToUpperInvariant()}_PASSWORD" : connection.SecretKeyName);
            }
        }
        else
        {
            var label = connection.ConnectionType == "Sqlite" ? "Path to the SQLite file" : "Path to the configuration directory";
            connection.ConfigPath = Prompts.Text(label, connection.ConfigPath);
        }

        answers.OtherConnections.Add(connection);
    }

    private static void Remove(WizardAnswers answers)
    {
        var names = answers.OtherConnections.Select(c => c.Name).ToList();
        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(ReferenceTheme.Markup("Remove which connection?"))
                .HighlightStyle(ReferenceTheme.Selected)
                .AddChoices(names));

        answers.OtherConnections.RemoveAll(c => c.Name == selected);
    }
}
