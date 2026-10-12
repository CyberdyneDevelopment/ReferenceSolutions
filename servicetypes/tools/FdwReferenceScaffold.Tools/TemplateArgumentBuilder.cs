using System.Collections.Generic;

namespace FdwReferenceScaffold.Tools;

/// <summary>
/// Turns a <see cref="WizardAnswers"/> into the argument list for
/// <c>dotnet new fdw-ref-api-host</c>. Only symbols whose value has moved off the
/// template's own default are emitted, so the printed command stays readable and a
/// template default changing upstream does not silently get overridden by a stale wizard.
/// </summary>
internal static class TemplateArgumentBuilder
{
    public static List<string> Build(WizardAnswers answers)
    {
        var defaults = new WizardAnswers();
        var args = new List<string> { "new", "fdw-ref-api-host", "-n", answers.Name };

        if (!string.IsNullOrWhiteSpace(answers.OutputDirectory))
        {
            args.Add("-o");
            args.Add(answers.OutputDirectory);
        }

        AddIfChanged(args, "--ConnectionType", answers.ConnectionType, defaults.ConnectionType);

        // Why unconditional rather than nested under "this connection needs a password", which is
        // where the raydar original puts it: over there SecretManager only names who supplies the
        // PlatformConfiguration password. Here the same symbol also decides which
        // FdwReferenceSecretManagers.*.Registration package the generated csproj references, which the
        // host needs for every OTHER connection's secrets too -- so a ManagedIdentity
        // PlatformConfiguration with a SqlAuth data connection still has to be able to set it.
        AddIfChanged(args, "--SecretManager", answers.SecretManager, defaults.SecretManager);
        AddIfChanged(args, "--ServerConfigRoot", answers.ServerConfigRoot, defaults.ServerConfigRoot);

        if (answers.IsServerBacked)
        {
            AddIfChanged(args, "--DbServer", answers.DbServer, defaults.DbServer);
            AddIfChanged(args, "--DbName", answers.DbName, defaults.DbName);
            AddIfChanged(args, "--DbPort", answers.DbPort, defaults.DbPort);
            AddIfChanged(args, "--DbUser", answers.DbUser, defaults.DbUser);
            AddIfChanged(args, "--AuthenticationType", answers.AuthenticationType, defaults.AuthenticationType);

            if (answers.NeedsConnectionSecret)
            {
                AddIfChanged(args, "--SecretKeyName", answers.SecretKeyName, defaults.SecretKeyName);
            }
        }
        else
        {
            AddIfChanged(args, "--ConfigPath", answers.ConfigPath, defaults.ConfigPath);
        }

        return args;
    }

    private static void AddIfChanged(List<string> args, string flag, string value, string defaultValue)
    {
        if (value == defaultValue)
        {
            return;
        }

        args.Add(flag);
        args.Add(value);
    }
}
