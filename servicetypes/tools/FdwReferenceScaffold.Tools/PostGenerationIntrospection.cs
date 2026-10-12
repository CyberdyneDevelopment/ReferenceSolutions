using System;
using System.Threading.Tasks;
using Spectre.Console;

namespace FdwReferenceScaffold.Tools;

/// <summary>
/// After `dotnet new` writes configurationSchema.json, patches in the "other connections" and
/// attempts a real introspection pass for every MsSql connection whose values look real —
/// PlatformConfiguration included — so DataStores don't all start with an empty Paths array.
/// </summary>
internal static class PostGenerationIntrospection
{
    public static async Task Run(WizardAnswers answers)
    {
        var path = ConfigurationSchemaPatcher.ResolvePath(answers);

        if (!System.IO.File.Exists(path))
        {
            // Why a message and not an exception: generation succeeded, so the useful outcome is
            // already on disk. Only the optional follow-up is impossible, and saying where it
            // looked is what lets a changed template layout be spotted immediately.
            AnsiConsole.MarkupLine($"[grey]Skipped configuration patching: no configurationSchema.json at {path}.[/]");
            return;
        }

        if (ConfigurationSchemaPatcher.AddOtherConnections(path, answers.OtherConnections))
        {
            AnsiConsole.MarkupLine(ReferenceTheme.AccentMarkup($"Added {answers.OtherConnections.Count} other connection(s) to configurationSchema.json."));
        }

        await TryIntrospect(path, "PlatformConfiguration", answers.ConnectionType, answers.DbServer, answers.DbName, answers.DbPort, answers.AuthenticationType, answers.DbUser, ResolvePlatformPassword(answers)).ConfigureAwait(false);

        foreach (var connection in answers.OtherConnections)
        {
            var password = connection.NeedsSecret
                ? Environment.GetEnvironmentVariable(ConfigurationSchemaPatcher.EnvSecretPrefix + connection.SecretKeyName)
                : null;
            await TryIntrospect(path, connection.Name, connection.ConnectionType, connection.DbServer, connection.DbName, connection.DbPort, connection.AuthenticationType, connection.DbUser, password).ConfigureAwait(false);
        }
    }

    private static string? ResolvePlatformPassword(WizardAnswers answers)
    {
        if (!answers.NeedsConnectionSecret || answers.SecretManager != "EnvironmentVariable")
        {
            return null;
        }

        return Environment.GetEnvironmentVariable(ConfigurationSchemaPatcher.EnvSecretPrefix + answers.SecretKeyName);
    }

    private static async Task TryIntrospect(
        string configurationSchemaPath,
        string dataStoreName,
        string connectionType,
        string server,
        string database,
        string port,
        string authenticationType,
        string user,
        string? password)
    {
        var result = await DatabaseIntrospector.TryIntrospect(connectionType, server, database, port, authenticationType, user, password).ConfigureAwait(false);

        if (result.Succeeded)
        {
            ConfigurationSchemaPatcher.ApplyIntrospectedPaths(configurationSchemaPath, dataStoreName, result.Paths);
            AnsiConsole.MarkupLine(ReferenceTheme.AccentMarkup($"Introspected {dataStoreName}: {result.Paths.Count} schema path(s) written."));
        }
        else
        {
            AnsiConsole.MarkupLine($"[grey]Skipped introspecting {dataStoreName}: {result.SkipReason}.[/]");
        }
    }
}
