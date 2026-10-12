using System.Collections.Generic;
using Spectre.Console;

namespace FdwReferenceScaffold.Tools.Steps;

/// <summary>
/// The two host-level symbols that are reference-only and have no raydar counterpart: where
/// ServerConfiguration's boot-time rows live, and which secret manager the host registers.
/// </summary>
internal static class HostStep
{
    private static readonly IReadOnlyList<(string Value, string Description)> SecretManagers =
    [
        ("EnvironmentVariable", "Read secrets from environment variables (FDW_SECRET_{KEY})."),
        ("MsSql", "Read secrets from sec.Secret in ConfigurationDb."),
    ];

    public static void Run(WizardAnswers answers)
    {
        AnsiConsole.Write(ReferenceTheme.Rule("Host configuration"));

        answers.ServerConfigRoot = Prompts.Text(
            "ServerConfiguration root directory (the host's own boot-time rows, read as files before ConfigurationDb is reachable)",
            answers.ServerConfigRoot);

        answers.SecretManager = Prompts.Choice(
            "Which secret manager does this host register?",
            SecretManagers,
            answers.SecretManager);

        // Why this warning rather than removing the MsSql choice outright: the template offers it
        // deliberately -- it is the right answer for every secret EXCEPT the one that unlocks the
        // store the secrets are in. A SqlAuth PlatformConfiguration resolves its own password
        // through this manager, so choosing MsSql there asks the host to read sec.Secret out of a
        // ConfigurationDb it cannot open yet. The template's own README says the same; a wizard
        // that silently let you pick it would be the only place that didn't.
        if (answers.SecretManager == "MsSql" && answers.NeedsConnectionSecret)
        {
            AnsiConsole.MarkupLine(ReferenceTheme.DangerMarkup(
                "PlatformConfiguration uses " + answers.AuthenticationType + ", which needs a password, and MsSql secrets live in the ConfigurationDb that password opens."));
            AnsiConsole.MarkupLine("[grey]Pick EnvironmentVariable for this host, or an authentication type that carries no password.[/]");
        }
    }
}
