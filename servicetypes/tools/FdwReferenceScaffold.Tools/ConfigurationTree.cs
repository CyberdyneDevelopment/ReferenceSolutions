using Spectre.Console;

namespace FdwReferenceScaffold.Tools;

/// <summary>Builds a full, navigable walk of everything the wizard has set so far.</summary>
internal static class ConfigurationTree
{
    public static Tree Build(WizardAnswers answers)
    {
        var tree = new Tree(ReferenceTheme.Markup("fdw-ref-api-host configuration"))
        {
            Style = new Style(ReferenceTheme.Border),
        };

        var project = tree.AddNode(Label("Project"));
        project.AddNode(Value("Name", answers.Name));
        project.AddNode(Value("Output directory", string.IsNullOrWhiteSpace(answers.OutputDirectory) ? "(default)" : answers.OutputDirectory));

        var platform = tree.AddNode(Label("Platform Configuration connection"));
        platform.AddNode(Value("Type", answers.ConnectionType));
        if (answers.IsServerBacked)
        {
            platform.AddNode(Value("Server", answers.DbServer));
            platform.AddNode(Value("Database", answers.DbName));
            platform.AddNode(Value("Port", answers.DbPort));
            platform.AddNode(Value("Login", answers.DbUser));
            platform.AddNode(Value("Authentication", answers.AuthenticationType));
            if (answers.NeedsConnectionSecret)
            {
                platform.AddNode(Value("Secret key", answers.SecretKeyName));
            }
        }
        else
        {
            platform.AddNode(Value("Path", answers.ConfigPath));
        }

        var otherConnections = tree.AddNode(Label($"Other connections ({answers.OtherConnections.Count})"));
        foreach (var connection in answers.OtherConnections)
        {
            var node = otherConnections.AddNode(Value(connection.Name, connection.ConnectionType));
            if (connection.IsServerBacked)
            {
                node.AddNode(Value("Server", connection.DbServer));
                node.AddNode(Value("Database", connection.DbName));
                node.AddNode(Value("Authentication", connection.AuthenticationType));
            }
            else
            {
                node.AddNode(Value("Path", connection.ConfigPath));
            }
        }

        var host = tree.AddNode(Label("Host configuration"));
        host.AddNode(Value("Secret manager", answers.SecretManager));
        host.AddNode(Value("ServerConfiguration root", answers.ServerConfigRoot));

        return tree;
    }

    private static string Label(string text) => ReferenceTheme.Markup(text);

    private static string Value(string label, string value) => $"{Muted(label)}: {ReferenceTheme.AccentMarkup(value)}";

    private static string Muted(string text) => $"[grey]{text}[/]";
}
