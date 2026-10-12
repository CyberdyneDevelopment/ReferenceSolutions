using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.SecretManagers.Abstractions;
using FdwReferenceSecretManagers.HashiCorpVault.Auth;
using FdwReferenceSecretManagers.HashiCorpVault.Engines;

namespace FdwReferenceSecretManagers.HashiCorpVault.Configuration;

/// <summary>
/// Typed configuration body for a HashiCorp Vault secret manager (<c>sec.HashiCorpVaultSecretManager</c>).
/// </summary>
/// <remarks>
/// <para>
/// Two engines are supported and they differ in kind, not just in path — see <see cref="Engine"/>.
/// <c>KeyValue</c> reads a secret somebody stored. <c>Database</c> asks Vault to <em>generate</em> a
/// short-lived database credential on the spot, which is the same "issued, not shared" model the
/// managed identity domain applies to service-to-service calls.
/// </para>
/// <para>
/// No credential is stored on this row. <see cref="AuthMethod"/> names how this process authenticates
/// to Vault, and the secret that authentication needs (a token, an AppRole secret id) is itself
/// resolved through <see cref="AuthSecretManagerName"/>/<see cref="AuthSecretKeyName"/> — or, for
/// workload-identity auth methods, is not a stored secret at all.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "SecretManager", ServiceType = "HashiCorpVault")]
public partial class HashiCorpVaultConfiguration : ISecretManagerImplementationConfiguration
{
    /// <summary>Initializes a new instance of the <see cref="HashiCorpVaultConfiguration"/> class.</summary>
    public HashiCorpVaultConfiguration()
    {
        Implementation = "HashiCorpVault";
    }

    /// <summary>Gets or sets the durable logical identity across versions. No default — the database assigns identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name this configuration is resolved by.</summary>
    // Why it maps with no column of its own: the name is the domain's, and there is one
    // name for a configured member. The domain provider joins the domain row to this one,
    // and the join's result set is what carries it in.
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <summary>Gets or sets the Vault base address (e.g. <c>https://vault.example.dev:8200</c>).</summary>
    public string? Address { get; set; }

    /// <summary>Gets or sets the Vault namespace (Enterprise); null on OSS.</summary>
    public string? VaultNamespace { get; set; }

    /// <summary>
    /// Gets or sets the <c>VaultSecretEngines</c> TypeOption name selecting what a read means —
    /// <c>KeyValue</c> to fetch a stored secret, <c>Database</c> to have Vault issue a fresh
    /// short-lived database credential.
    /// </summary>
    [ValuesFrom(typeof(VaultSecretEngines))]
    public string? Engine { get; set; }

    /// <summary>Gets or sets the engine mount path (e.g. <c>secret</c> for KV, <c>database</c> for the database engine).</summary>
    public string? Mount { get; set; }

    /// <summary>
    /// Gets or sets the <c>VaultAuthMethods</c> TypeOption name describing how this process
    /// authenticates to Vault (e.g. <c>Token</c>, <c>AppRole</c>, <c>Jwt</c>).
    /// </summary>
    [ValuesFrom(typeof(VaultAuthMethods))]
    public string? AuthMethod { get; set; }

    /// <summary>Gets or sets the auth mount path when it differs from the method's default (e.g. a second AppRole mount).</summary>
    public string? AuthMount { get; set; }

    /// <summary>Gets or sets the AppRole role id, or the JWT role name, depending on <see cref="AuthMethod"/>.</summary>
    public string? AuthRoleId { get; set; }

    /// <summary>Gets or sets the name of the secret manager holding this Vault login's own secret.</summary>
    /// <remarks>
    /// Null for auth methods that present a workload assertion rather than a stored secret. Bootstrapping
    /// one secret manager from another is deliberate: the alternative is a Vault credential sitting in
    /// PlatformConfiguration, which defeats the point of using Vault.
    /// </remarks>
    public string? AuthSecretManagerName { get; set; }

    /// <summary>Gets or sets the key under which this Vault login's secret is stored.</summary>
    public string? AuthSecretKeyName { get; set; }

    /// <summary>
    /// Gets or sets the path to read the assertion from, for workload-identity auth methods (e.g. the
    /// environment variable carrying a CI job's JWT).
    /// </summary>
    public string? AuthAssertionLocation { get; set; }

    /// <summary>Gets or sets an optional human-readable description for this configuration.</summary>
    public string? Description { get; set; }

    // ── Tenant / visibility / audit ──────────────────────────────────────────
    // Why: no value defaults — a missing tenant/visibility/audit value must read as its
    // DB-configured null, never a silently-assumed default.

    /// <summary>Gets or sets the tenant identifier for tenant isolation. Null means system-wide.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets the optional visibility group identifier.</summary>
    public Guid? VisibilityGroupId { get; set; }

    /// <summary>Gets or sets the source create date (set by DB).</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the user who created this record.</summary>
    public string? CreateBy { get; set; }

    /// <summary>Gets or sets the on-behalf-of user for create (impersonation).</summary>
    public string? CreateOnBehalfOf { get; set; }

    /// <summary>Gets or sets the last modification date.</summary>
    public DateTimeOffset? ModifyDate { get; set; }

    /// <summary>Gets or sets the user who last modified this record.</summary>
    public string? ModifyBy { get; set; }

    /// <summary>Gets or sets the on-behalf-of user for modify (impersonation).</summary>
    public string? ModifyOnBehalfOf { get; set; }
}
