using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Hosts.Abstractions;

namespace FdwReferenceHosts.SqlServerEndpoint.Configuration;

/// <summary>
/// The host implementation of the SQL Server endpoint domain: where a host listens for SQL Server clients,
/// the server it presents itself as, the flow a SQL login runs, and the secrets that hold its certificate.
/// </summary>
/// <remarks>
/// Every setting is required. What a vendor's client is told the server is — its name, version, edition,
/// collation — and what it may do — how many connect, how long a login may take — is configuration, not a
/// default this package picks.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "SqlServerEndpoint", ServiceType = "Host")]
public sealed partial class SqlServerEndpointHostConfiguration : ISqlServerEndpointImplementationConfiguration
{
    /// <summary>Gets or sets the identifier.</summary>
    public Guid Id { get; set; }

    // Why it maps with no column of its own: the name is the domain's, and there is one
    // name for a configured member. The domain provider joins the domain row to this one,
    // and the join's result set is what carries it in.
    /// <summary>Gets or sets the name, carried in from the domain row.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the domain this row belongs to.</summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Gets or sets the implementation the domain row names.</summary>
    public string Implementation { get; set; } = string.Empty;

    /// <summary>Gets or sets the address listened on, such as 0.0.0.0 for every interface.</summary>
    public string ListenAddress { get; set; } = string.Empty;

    /// <summary>Gets or sets the TCP port listened on; SQL Server's own is 1433.</summary>
    public int Port { get; set; }

    /// <summary>Gets or sets the name @@SERVERNAME and SERVERPROPERTY('ServerName') answer.</summary>
    public string ServerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the product version presented, such as 16.0.4135.4.</summary>
    public string ProductVersion { get; set; } = string.Empty;

    /// <summary>Gets or sets the edition SERVERPROPERTY('Edition') answers.</summary>
    public string Edition { get; set; } = string.Empty;

    /// <summary>Gets or sets the engine edition SERVERPROPERTY('EngineEdition') answers.</summary>
    public int EngineEdition { get; set; }

    /// <summary>Gets or sets the server and database collation, by SQL Server name.</summary>
    public string Collation { get; set; } = string.Empty;

    /// <summary>Gets or sets the session language, such as us_english.</summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the one database this server serves.</summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>Gets or sets the authentication flow a SQL login runs; it must deliver a session.</summary>
    public string LoginFlowName { get; set; } = string.Empty;

    /// <summary>Gets or sets the secret manager the certificate is read from.</summary>
    public string SecretManagerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the secret holding the TLS certificate, as a base64 PKCS#12 file.</summary>
    public string CertificateSecretName { get; set; } = string.Empty;

    /// <summary>Gets or sets the secret holding the certificate file's password.</summary>
    public string CertificatePasswordSecretName { get; set; } = string.Empty;

    /// <summary>Gets or sets the largest request message read, in bytes.</summary>
    public int MaximumMessageBytes { get; set; }

    /// <summary>Gets or sets how many clients may be connected at once.</summary>
    public int MaximumConnections { get; set; }

    /// <summary>Gets or sets how many seconds a new connection has to finish its handshake and login.</summary>
    public int LoginTimeoutSeconds { get; set; }
}
