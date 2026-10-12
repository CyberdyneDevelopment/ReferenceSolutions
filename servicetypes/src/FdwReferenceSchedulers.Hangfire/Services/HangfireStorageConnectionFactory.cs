using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.MsSql;
using Fdw.Services.Connections.MsSql.Authentication;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceSchedulers.Hangfire.Logging;

namespace FdwReferenceSchedulers.Hangfire.Services;

/// <summary>
/// Builds the raw ADO.NET connection Hangfire's own storage needs, from a connection this platform
/// already has configured — <see cref="Configuration.HangfireSchedulerConfiguration.StorageConnectionName"/>
/// names it, and <see cref="IConnectionConfigurationProvider"/> supplies its settings, so the
/// connection is configured in exactly one place like every other connection.
/// </summary>
/// <remarks>
/// Why this builds a <see cref="SqlConnection"/> directly, against the rule that DataGateway is the
/// only data access path and raw connections belong to connection implementation projects: this is
/// a requirement of the external class. Hangfire owns its own storage — its schema, its migrations,
/// its queries — and <c>UseSqlServerStorage</c> accepts only a connection string or a
/// <c>Func&lt;DbConnection&gt;</c>. FDW's own connection abstraction cannot be handed to it, so
/// something has to compose a real ADO.NET connection. That is kept here, <c>internal</c> to this
/// package, so the capability exists exactly where the external library forces it and nowhere else.
/// </remarks>
/// <remarks>
/// Public rather than internal only because the <c>[Implementation]</c> that wires Hangfire's
/// storage lives in the sibling <c>.Registration</c> assembly, and reference implementations get no
/// <c>InternalsVisibleTo</c> — anything needed to compose the service has to be public API. The
/// connection-string composition itself stays private to this class.
/// </remarks>
public sealed class HangfireStorageConnectionFactory
{
    private readonly IConnectionConfigurationProvider _connections;
    private readonly ISecretManagerProvider _secrets;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="HangfireStorageConnectionFactory"/> class.</summary>
    public HangfireStorageConnectionFactory(
        IConnectionConfigurationProvider connections,
        ISecretManagerProvider secrets,
        ILogger<HangfireStorageConnectionFactory>? logger = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _logger = logger ?? NullLogger<HangfireStorageConnectionFactory>.Instance;
    }

    /// <summary>
    /// Resolves the named connection once and returns the factory Hangfire calls for each connection
    /// it needs. Resolution happens here, at setup, so the per-connection callback stays synchronous
    /// as Hangfire requires.
    /// </summary>
    /// <param name="connectionName">The configured connection Hangfire's storage lives in.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<IGenericResult<Func<DbConnection>>> Create(
        string connectionName, CancellationToken cancellationToken = default)
    {
        var configResult = await _connections.Get(connectionName, cancellationToken).ConfigureAwait(false);
        if (!configResult.IsSuccess || configResult.Value is null)
        {
            return GenericResult<Func<DbConnection>>.Failure(
                HangfireSchedulingLog.NativeCallFailed(_logger,
                    new InvalidOperationException($"Connection '{connectionName}' is not configured."), connectionName));
        }

        // Get returns the connection's IMPLEMENTATION configuration directly -- the domain row that
        // named it is how this was found, not part of the answer. Testing configResult.Value itself
        // for the implementation type is what current IDomainConfigurationProvider callers do.
        if (configResult.Value is not MsSqlConnectionConfiguration msSql)
        {
            return GenericResult<Func<DbConnection>>.Failure(
                HangfireSchedulingLog.NativeCallFailed(_logger,
                    new InvalidOperationException(
                        $"Connection '{connectionName}' is a {configResult.Value?.GetType().Name ?? "(no implementation)"}; Hangfire's SQL Server storage needs an MsSql connection."),
                    connectionName));
        }

        var authType = MsSqlAuthenticationTypes.ByName(msSql.AuthenticationType);
        if (authType == MsSqlAuthenticationTypes.NotFound)
        {
            return GenericResult<Func<DbConnection>>.Failure(
                HangfireSchedulingLog.NativeCallFailed(_logger,
                    new InvalidOperationException($"Connection '{connectionName}' names authentication type '{msSql.AuthenticationType}', which is not a registered MsSqlAuthenticationTypes option."),
                    connectionName));
        }

        // The auth type owns its own fragment -- this never switches on the authentication type, it
        // looks it up. BuildAuthFragment itself is synchronous now: the auth type answers what
        // secret it needs via RequiredSecret, fetching it is this caller's job (same split
        // MsSqlConnectionProvider makes for every other MsSql connection), and only the resolved
        // value is handed to BuildAuthFragment.
        var values = new Dictionary<string, string?>(msSql.AdditionalProperties, StringComparer.OrdinalIgnoreCase);
        var requirement = authType.RequiredSecret(values);
        if (!requirement.IsSuccess)
            return requirement.ToNewResult<Func<DbConnection>>();

        string? resolvedSecret = null;
        if (!requirement.Value.IsEmpty)
        {
            var manager = await _secrets.Get(requirement.Value.ManagerName, cancellationToken).ConfigureAwait(false);
            if (!manager.IsSuccess || manager.Value is null)
                return manager.ToNewResult<Func<DbConnection>>();

            var secret = await manager.Value
                .Execute(GetSecretManagerCommand.Latest(null, requirement.Value.KeyName), cancellationToken)
                .ConfigureAwait(false);
            if (!secret.IsSuccess || secret.Value is null)
                return secret.ToNewResult<Func<DbConnection>>();

            resolvedSecret = secret.Value.GetStringValue();
        }

        var fragmentResult = authType.BuildAuthFragment(values, resolvedSecret);
        if (!fragmentResult.IsSuccess || fragmentResult.Value is null)
        {
            return GenericResult<Func<DbConnection>>.Failure(
                HangfireSchedulingLog.NativeCallFailed(_logger,
                    new InvalidOperationException(fragmentResult.CurrentMessage ?? $"Could not build the authentication fragment for '{connectionName}'."),
                    connectionName));
        }

        var connectionString = BuildBaseConnectionString(msSql) + fragmentResult.Value;
        return GenericResult<Func<DbConnection>>.Success(() => new SqlConnection(connectionString));
    }

    private static string BuildBaseConnectionString(MsSqlConnectionConfiguration config)
    {
        var builder = new StringBuilder();
        builder.Append(CultureInfo.InvariantCulture, $"Server={config.Server}");

        if (!string.IsNullOrWhiteSpace(config.InstanceName))
        {
            builder.Append(CultureInfo.InvariantCulture, $"\\{config.InstanceName}");
        }
        else if (config.Port != 1433)
        {
            builder.Append(CultureInfo.InvariantCulture, $",{config.Port}");
        }

        builder.Append(CultureInfo.InvariantCulture, $";Database={config.Database};");
        builder.Append(CultureInfo.InvariantCulture, $"TrustServerCertificate={config.TrustServerCertificate};");
        builder.Append(CultureInfo.InvariantCulture, $"Encrypt={config.Encrypt};");
        builder.Append(CultureInfo.InvariantCulture, $"Connect Timeout={config.ConnectionTimeoutSeconds};");
        builder.Append(CultureInfo.InvariantCulture, $"Pooling={config.EnableConnectionPooling};");
        builder.Append(CultureInfo.InvariantCulture, $"Min Pool Size={config.MinPoolSize};");
        builder.Append(CultureInfo.InvariantCulture, $"Max Pool Size={config.MaxPoolSize};");
        builder.Append(CultureInfo.InvariantCulture, $"MultipleActiveResultSets={config.EnableMultipleActiveResultSets};");

        if (!string.IsNullOrWhiteSpace(config.ApplicationName))
        {
            builder.Append(CultureInfo.InvariantCulture, $"Application Name={config.ApplicationName};");
        }

        return builder.ToString();
    }
}
