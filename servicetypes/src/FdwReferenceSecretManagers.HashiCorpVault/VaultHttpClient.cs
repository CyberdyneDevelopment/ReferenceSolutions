using System;
using Microsoft.Extensions.DependencyInjection;

namespace FdwReferenceSecretManagers.HashiCorpVault;

/// <summary>
/// The named <c>HttpClient</c> the Vault secret manager uses.
/// </summary>
/// <remarks>
/// Registered through <c>IHttpClientFactory</c> so socket handlers are pooled and recycled. A secret
/// manager that news up its own client per instance either exhausts sockets or pins stale DNS, and
/// the thing every other service reads its credentials through must not become the reason they stop.
/// </remarks>
public static class VaultHttpClient
{
    /// <summary>The name this client is registered and resolved under.</summary>
    public const string Name = "Fdw.SecretManagers.HashiCorpVault";

    /// <summary>
    /// How long a Vault call may take before it is treated as unreachable.
    /// </summary>
    /// <remarks>
    /// Why bounded: a secret read sits in front of whatever needed the secret. An unbounded wait turns
    /// a slow Vault into a stalled application rather than a clean, diagnosable failure. A deployment
    /// sets this before the host registers services.
    /// </remarks>
    public static TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Registers the named client, idempotently.</summary>
    /// <param name="services">The service collection to register into.</param>
    public static void Register(IServiceCollection services)
        => services.AddHttpClient(Name, client => client.Timeout = Timeout);
}
