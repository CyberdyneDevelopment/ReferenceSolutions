using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using FdwReferenceSecretManagers.HashiCorpVault.Auth;
using FdwReferenceSecretManagers.HashiCorpVault.Engines;

namespace FdwReferenceSecretManagers.HashiCorpVault;

/// <summary>
/// Everything <see cref="VaultApiClient"/> needs for one read: which Vault, which engine, and how to
/// obtain the credential it logs in with. Resolved once from configuration so the client never reads
/// configuration itself.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ResolveAuthSecret"/> is a delegate rather than an already-resolved string on purpose.
/// The login secret usually comes from another secret manager, whose read is asynchronous, while
/// <c>IServiceFactory.Create</c> is synchronous across every FDW domain. Resolving eagerly would mean
/// blocking on a task inside factory construction — a deadlock risk the VSTHRD002 analyzer refuses,
/// and rightly. Deferring it also means a workload assertion is read when it is used rather than when
/// the manager was built, which matters because those rotate.
/// </para>
/// <para>
/// <b>Known gap, stated rather than papered over.</b> When <see cref="Engine"/> issues credentials
/// (Vault's database engine), Vault mints BOTH a username and a password, and this secret manager
/// returns the password as the secret value with the username in
/// <c>SecretValue.Metadata["username"]</c>. The connection layer today takes its username from the
/// connection's own configuration and reads only the password
/// (<c>SqlAuthConfiguration.BuildAuthFragment</c>), so a Vault role that rotates the username will not
/// connect until that layer consumes the metadata. Static-username Vault roles work today. That is a
/// gap in the connection mechanism, and the fix belongs there — routing a Vault-minted username
/// through a side channel at one call site is the symptom patch that leaves every other caller broken.
/// </para>
/// </remarks>
public sealed class VaultReadContext
{
    /// <summary>Initializes a new instance of the <see cref="VaultReadContext"/> class.</summary>
    /// <param name="configurationName">The secret manager configuration performing the read, for logging.</param>
    /// <param name="address">The Vault base address.</param>
    /// <param name="mount">The secret engine mount path.</param>
    /// <param name="engine">The secret engine to read through.</param>
    /// <param name="authMethod">How this process authenticates to Vault.</param>
    /// <param name="resolveAuthSecret">Obtains the login secret or assertion, asynchronously, when a login is needed.</param>
    /// <param name="authRoleId">The AppRole role id or JWT role name.</param>
    /// <param name="authMount">The auth mount path, when it differs from the method's default.</param>
    /// <param name="vaultNamespace">The Vault Enterprise namespace, if any.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="engine"/>, <paramref name="authMethod"/>, or <paramref name="resolveAuthSecret"/> is null.</exception>
    public VaultReadContext(
        string configurationName,
        string address,
        string mount,
        IVaultSecretEngine engine,
        IVaultAuthMethod authMethod,
        Func<CancellationToken, Task<IGenericResult<string>>> resolveAuthSecret,
        string? authRoleId = null,
        string? authMount = null,
        string? vaultNamespace = null)
    {
        ConfigurationName = configurationName;
        Address = address;
        Mount = mount;
        Engine = engine ?? throw new ArgumentNullException(nameof(engine));
        AuthMethod = authMethod ?? throw new ArgumentNullException(nameof(authMethod));
        ResolveAuthSecret = resolveAuthSecret ?? throw new ArgumentNullException(nameof(resolveAuthSecret));
        AuthRoleId = authRoleId;
        AuthMount = authMount;
        VaultNamespace = vaultNamespace;
    }

    /// <summary>Gets the secret manager configuration performing the read.</summary>
    public string ConfigurationName { get; }

    /// <summary>Gets the Vault base address.</summary>
    public string Address { get; }

    /// <summary>Gets the secret engine mount path.</summary>
    public string Mount { get; }

    /// <summary>Gets the secret engine reads go through.</summary>
    public IVaultSecretEngine Engine { get; }

    /// <summary>Gets how this process authenticates to Vault.</summary>
    public IVaultAuthMethod AuthMethod { get; }

    /// <summary>Gets the deferred resolution of the login secret. Produces a credential — never logged.</summary>
    public Func<CancellationToken, Task<IGenericResult<string>>> ResolveAuthSecret { get; }

    /// <summary>Gets the AppRole role id or JWT role name.</summary>
    public string? AuthRoleId { get; }

    /// <summary>Gets the auth mount path, when it differs from the method's default.</summary>
    public string? AuthMount { get; }

    /// <summary>Gets the Vault Enterprise namespace, if any.</summary>
    public string? VaultNamespace { get; }
}
