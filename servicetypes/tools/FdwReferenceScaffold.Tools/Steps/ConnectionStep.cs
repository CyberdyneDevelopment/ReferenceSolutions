using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Connections.MsSql.Authentication;
using Fdw.Services.Connections.PostgreSql.Authentication;
using Spectre.Console;

namespace FdwReferenceScaffold.Tools.Steps;

/// <summary>
/// Where the host reads ConfigurationDb from, plus authentication to it and, when that
/// authentication needs a password, the secret manager that supplies one.
/// </summary>
internal static class ConnectionStep
{
    private static readonly IReadOnlyList<(string Value, string Description)> ConnectionTypes =
    [
        ("MsSql", "SQL Server. Asks for server, database, port, login and how the password is supplied."),
        ("PostgreSql", "PostgreSQL. Same details as SQL Server."),
        ("Sqlite", "A SQLite file. Asks only for the file path."),
        ("FileSystem", "Configuration held as files on disk. Asks only for the directory path."),
    ];

    public static void Run(WizardAnswers answers)
    {
        AnsiConsole.Write(ReferenceTheme.Rule("Platform Configuration connection"));

        var previousConnectionType = answers.ConnectionType;
        answers.ConnectionType = Prompts.Choice(
            "Where does this host read its configuration from?",
            ConnectionTypes,
            answers.ConnectionType);

        if (answers.IsServerBacked)
        {
            answers.DbServer = Prompts.Text("ConfigurationDb host (${FDW_CONFIG_SERVER} resolves from the environment)", answers.DbServer);
            answers.DbName = Prompts.Text("ConfigurationDb catalogue name", answers.DbName);
            answers.DbPort = ConnectionDefaults.PromptPort(answers.ConnectionType, answers.DbPort);
            answers.DbUser = Prompts.Text("Database login", answers.DbUser);

            var authenticationTypes = AuthenticationOptions(answers.ConnectionType);

            // MsSql and PostgreSql don't share authentication method names -- switching
            // ConnectionType away from the value the current pick belongs to must not carry a
            // stale choice (e.g. "SqlAuth") into the other connection's option list.
            if (answers.ConnectionType != previousConnectionType
                && authenticationTypes.All(option => option.Value != answers.AuthenticationType))
            {
                answers.AuthenticationType = authenticationTypes[0].Value;
            }

            answers.AuthenticationType = Prompts.Choice(
                "How does this host authenticate to ConfigurationDb?",
                authenticationTypes,
                answers.AuthenticationType);

            if (RequiresSecret(answers.ConnectionType, answers.AuthenticationType))
            {
                // SecretManager isn't asked here: MsSql (read the password from sec.Secret in
                // ConfigurationDb) can never work for this specific connection's own password --
                // it can't read sec.Secret until it can already open this same connection. See
                // WizardAnswers.SecretManager. This is what makes PlatformConfiguration the one
                // place this check matters: every OTHER connection is read once ConfigurationDb
                // and its secret manager already exist, so its secret -- whatever the connection's
                // own authentication method actually needs -- just goes in that secret manager
                // under whatever key the developer names, regardless of RequiresSecret.
                answers.SecretKeyName = Prompts.Text("Logical name of the connection password", answers.SecretKeyName);
            }
        }
        else
        {
            var label = answers.ConnectionType == "Sqlite"
                ? "Path to the SQLite file"
                : "Path to the configuration directory";
            answers.ConfigPath = Prompts.Text(label, answers.ConfigPath);
        }
    }

    /// <summary>
    /// Whether the given authentication method on the given connection type ever carries a
    /// password -- read directly off that method's own RequiresSecret rather than a hand-copied
    /// table, so a new or changed FDW authentication method is correct here automatically instead
    /// of silently drifting out of sync.
    /// </summary>
    public static bool RequiresSecret(string connectionType, string authenticationType) => connectionType switch
    {
        "MsSql" => MsSqlAuthenticationTypes.All().Any(option => option.Name == authenticationType && option.RequiresSecret),
        "PostgreSql" => PostgreSqlAuthenticationTypes.All().Any(option => option.Name == authenticationType && option.RequiresSecret),
        _ => false,
    };

    /// <summary>The real authentication choices for a connection type, read off its FDW TypeCollection.</summary>
    public static IReadOnlyList<(string Value, string Description)> AuthenticationOptions(string connectionType) => connectionType switch
    {
        "MsSql" => MsSqlAuthenticationTypes.All().Select(option => (option.Name, option.Description)).ToList(),
        "PostgreSql" => PostgreSqlAuthenticationTypes.All().Select(option => (option.Name, option.Description)).ToList(),
        _ => [],
    };
}
