using Fdw.Collections.Attributes;

namespace FdwReferenceSecretManagers.HashiCorpVault.Engines;

/// <summary>
/// Vault's database engine — asks Vault to generate a fresh, lease-bound database credential.
/// </summary>
/// <remarks>
/// <para>
/// A read here is not a lookup, it is an issuance: Vault creates a new database user, returns its
/// username and password, and revokes it when the lease ends. Nobody, including this process, holds
/// a durable database password. This is the same "issued, not shared" model the managed identity
/// domain applies to service-to-service calls, applied to the database connection.
/// </para>
/// <para>
/// The secret key is a Vault ROLE name, not a path — <c>database/creds/&lt;role&gt;</c> — and the role
/// is what binds the issued credential to a set of database grants.
/// </para>
/// </remarks>
[TypeOption(typeof(VaultSecretEngines), "Database")]
public sealed class DatabaseVaultSecretEngine : VaultSecretEngineBase
{
    /// <summary>Initializes a new instance of the <see cref="DatabaseVaultSecretEngine"/> class.</summary>
    public DatabaseVaultSecretEngine() : base("Database", issuesCredential: true)
    {
    }

    /// <inheritdoc/>
    public override string BuildReadPath(string mount, string secretKey) => $"{mount}/creds/{secretKey}";

    /// <inheritdoc/>
    public override string ValueField => "password";
}
