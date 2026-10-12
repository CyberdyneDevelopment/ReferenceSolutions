using System.Collections.Generic;

namespace FdwReferenceScaffold.Tools;

/// <summary>
/// Every fdw-ref-api-host template symbol the wizard can set, seeded with the same defaults as
/// templates/ReferenceApiHost/.template.config/template.json so an unedited answer never emits a flag.
/// </summary>
/// <remarks>
/// This is the reference symbol set, which is deliberately narrower than the raydar one this
/// wizard is ported from. fdw-ref-api-host has no external-identity, chain-mapped-role, tenant or
/// solution-naming symbols, so no field for them exists here -- carrying answers the template
/// cannot consume would print flags that make `dotnet new` fail. It also has no package-version
/// symbol: templates in this repo take their versions from Directory.Packages.props, and
/// verify-templates.sh fails the build if one pins its own. Three symbols are reference-only and
/// have no raydar counterpart: ServerConfigRoot, and the UseEnvSecrets/UseMsSqlSecrets pair
/// computed from SecretManager.
/// </remarks>
internal sealed class WizardAnswers
{
    public string Name { get; set; } = string.Empty;
    public string OutputDirectory { get; set; } = string.Empty;

    public string ConnectionType { get; set; } = "MsSql";
    public string DbServer { get; set; } = "${FDW_CONFIG_SERVER}";
    public string DbName { get; set; } = "devConfigurationDb";
    public string DbPort { get; set; } = "1433";
    public string DbUser { get; set; } = "fdw_config";
    public string ConfigPath { get; set; } = "/var/lib/fdw/configuration.db";

    public string AuthenticationType { get; set; } = "SqlAuth";
    public string SecretManager { get; set; } = "EnvironmentVariable";
    public string SecretKeyName { get; set; } = "CONFIG_PASSWORD";

    /// <summary>
    /// Root of the ServerConfiguration FileSystem connection -- the host's own boot-time rows, read
    /// before PlatformConfiguration is reachable. Reference-only; raydar's template has no equivalent.
    /// </summary>
    public string ServerConfigRoot { get; set; } = "server-config";

    /// <summary>
    /// Connections beyond PlatformConfiguration, patched into the generated configurationSchema.json
    /// after `dotnet new` runs (the template itself has no mechanism for a variable-length list).
    /// </summary>
    public List<ConnectionAnswer> OtherConnections { get; } = [];

    public bool IsServerBacked => ConnectionType is "MsSql" or "PostgreSql";
    public bool IsFileBacked => ConnectionType is "Sqlite" or "FileSystem";

    // Keys off both ConnectionType and AuthenticationType -- MsSql's SqlAuth and PostgreSql's
    // Password are the only two ConfigurationDb authentication methods that ever carry a
    // password, and the two connections don't share an authentication method name space.
    public bool NeedsConnectionSecret => Steps.ConnectionStep.RequiresSecret(ConnectionType, AuthenticationType);
}
