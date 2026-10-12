namespace FdwReferenceScaffold.Tools;

/// <summary>
/// One named connection beyond PlatformConfiguration — patched into the generated
/// configurationSchema.json's Connections and DataStores arrays after generation.
/// </summary>
internal sealed class ConnectionAnswer
{
    // Defaults use a "${Type.Property}" token, not string.Empty, for every value that always
    // gets written into the generated configurationSchema.json with no suppression logic behind
    // it (unlike WizardAnswers' blank-means-omit fields) — so a field the user skipped past shows
    // up as an obvious, greppable marker in the output instead of a silent empty string, and (for
    // Server/Database) fails loudly at the app's own ${VAR} startup resolution instead of quietly
    // resolving to nothing.
    public string Name { get; set; } = "${ConnectionAnswer.Name}";
    public string ConnectionType { get; set; } = "MsSql";

    public string DbServer { get; set; } = "${ConnectionAnswer.DbServer}";
    public string DbName { get; set; } = "${ConnectionAnswer.DbName}";
    public string DbPort { get; set; } = "1433";
    public string DbUser { get; set; } = "${ConnectionAnswer.DbUser}";
    public string AuthenticationType { get; set; } = "SqlAuth";
    public string SecretKeyName { get; set; } = "${ConnectionAnswer.SecretKeyName}";

    public string ConfigPath { get; set; } = "${ConnectionAnswer.ConfigPath}";

    public bool IsServerBacked => ConnectionType is "MsSql" or "PostgreSql";

    // Keys off both ConnectionType and AuthenticationType -- see ConnectionStep.RequiresSecret,
    // which this shares with PlatformConfiguration's own secret determination.
    public bool NeedsSecret => Steps.ConnectionStep.RequiresSecret(ConnectionType, AuthenticationType);
}
