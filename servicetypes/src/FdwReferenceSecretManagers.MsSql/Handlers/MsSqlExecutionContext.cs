using System;
using Fdw.Configuration;
using Fdw.Security.Hashing;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Handlers;
using FdwReferenceSecretManagers.MsSql.Configuration;
using Microsoft.Extensions.Logging;
using Fdw.Services.SecretManagers;

namespace FdwReferenceSecretManagers.MsSql.Handlers;

/// <summary>
/// MsSql specific execution context providing access to the <see cref="IConfigurationGateway"/>
/// used to read/write the secret manager's tables.
/// </summary>
internal sealed class MsSqlExecutionContext : ISecretManagerExecutionContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlExecutionContext"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <param name="configuration">The MsSql secret manager configuration.</param>
    /// <param name="gateway">The configuration gateway for executing data commands.</param>
    /// <param name="serviceId">The service identifier.</param>
    /// <param name="dataStoreName">
    /// The DataStore name the secret/user/token tables are addressed under (e.g. "PlatformConfiguration").
    /// </param>
    /// <param name="secretManagerName">
    /// The logical name of the secret manager (from the <c>SecretManagerConfiguration</c> header).
    /// After config-split, <c>MsSqlSecretManagerConfiguration.Name</c> returns <c>string.Empty</c>;
    /// the real name is threaded from the factory through this parameter.
    /// </param>
    /// <param name="passwordHasher">Optional password hasher for credential verification.</param>
    /// <param name="tokenHasher">Optional PAT hasher for API key verification.</param>
    /// <param name="tokenGenerator">Optional PAT generator for API key creation.</param>
    /// <param name="hmacKey">Optional HMAC key for API key hashing operations.</param>
    public MsSqlExecutionContext(
        ILogger logger,
        MsSqlSecretManagerConfiguration configuration,
        IConfigurationGateway gateway,
        string serviceId,
        string dataStoreName,
        string secretManagerName,
        IPasswordHasher? passwordHasher = null,
        IPersonalAccessTokenHasher? tokenHasher = null,
        IPersonalAccessTokenGenerator? tokenGenerator = null,
        string? hmacKey = null)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        MsSqlConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        ServiceId = serviceId ?? throw new ArgumentNullException(nameof(serviceId));
        DataStoreName = dataStoreName ?? throw new ArgumentNullException(nameof(dataStoreName));
        // Why: Name is a header field after config-split; use the threaded name or fall back to the GUID.
        SecretManagerName = !string.IsNullOrEmpty(secretManagerName) ? secretManagerName : serviceId;
        PasswordHasher = passwordHasher;
        TokenHasher = tokenHasher;
        TokenGenerator = tokenGenerator;
        HmacKey = hmacKey;
    }

    /// <inheritdoc />
    public ILogger Logger { get; }

    /// <inheritdoc />
    public IGenericConfiguration Configuration => MsSqlConfiguration;

    /// <summary>
    /// Gets the MsSql specific configuration.
    /// </summary>
    public MsSqlSecretManagerConfiguration MsSqlConfiguration { get; }

    /// <summary>
    /// Gets the logical name of this secret manager instance.
    /// </summary>
    /// <remarks>
    /// After config-split, <c>MsSqlSecretManagerConfiguration.Name</c> returns <c>string.Empty</c>
    /// because <c>Name</c> lives on the <c>SecretManagerConfiguration</c> header row. This property
    /// carries the real name threaded from the factory so handlers can use it in logging/diagnostics.
    /// </remarks>
    public string SecretManagerName { get; }

    /// <summary>
    /// Gets the configuration gateway used to execute data commands against PlatformConfiguration/AuthDb.
    /// </summary>
    public IConfigurationGateway Gateway { get; }

    /// <summary>
    /// Gets the DataStore name the secret/user/token tables are addressed under (e.g. "PlatformConfiguration").
    /// </summary>
    public string DataStoreName { get; }

    /// <summary>
    /// Gets the target addressing the sec.Secret table (schema/table come from configuration).
    /// </summary>
    public DataStoreTarget SecretTarget => new(DataStoreName, MsSqlConfiguration.Schema, MsSqlConfiguration.TableName);

    /// <summary>
    /// Gets the target addressing the usr.Users table.
    /// </summary>
    public DataStoreTarget UsersTarget => new(DataStoreName, "usr", "Users");

    /// <summary>
    /// Gets the target addressing the auth.PersonalAccessToken table, or null when the host has not
    /// named the store it lives in.
    /// </summary>
    /// <remarks>
    /// A different store from <see cref="DataStoreName"/>: this domain's configuration lives in one
    /// place and its personal access tokens in another. The name comes from
    /// <c>SecretManagerTypes.OperationalConnection</c>, which has no default because an operational
    /// store is a row inside the configuration store rather than something the schema declares.
    /// Null rather than a made-up name, so each caller reports it instead of querying a store nobody
    /// named.
    /// </remarks>
    // Why: intentionally an instance property (not static) for call-site symmetry with SecretTarget/
    // UsersTarget, even though this one target doesn't depend on instance state.
#pragma warning disable CA1822
    public DataStoreTarget? PersonalAccessTokenTarget
    {
        get
        {
            // Read once into a local: a property is not narrowed by the null check, and reading it
            // twice would let the two reads disagree.
            var store = SecretManagerTypes.OperationalConnection;
            return string.IsNullOrWhiteSpace(store)
                ? null
                : new DataStoreTarget(store, "auth", "PersonalAccessToken");
        }
    }
#pragma warning restore CA1822

    /// <inheritdoc />
    public string ServiceId { get; }

    /// <summary>
    /// Gets the password hasher for credential verification operations.
    /// </summary>
    public IPasswordHasher? PasswordHasher { get; }

    /// <summary>
    /// Gets the personal access token hasher for API key verification.
    /// </summary>
    public IPersonalAccessTokenHasher? TokenHasher { get; }

    /// <summary>
    /// Gets the personal access token generator for API key creation.
    /// </summary>
    public IPersonalAccessTokenGenerator? TokenGenerator { get; }

    /// <summary>
    /// Gets the HMAC key for API key hashing operations.
    /// </summary>
    public string? HmacKey { get; }
}
