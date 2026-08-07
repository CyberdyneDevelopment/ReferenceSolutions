using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.MsSql;
using ReferenceConnections.MsSql.DataVault.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

using ReferenceConnections.MsSql;

using Fdw.Services.DataVault;

namespace ReferenceConnections.MsSql.DataVault;

/// <summary>
/// SQL Server vault tier. Implements the abstract <c>Query</c>/<c>NonQuery</c> primitives over
/// <see cref="SqlCommand"/> using strictly parameterized ADO. This is the sanctioned raw-ADO
/// surface for vaults — exactly like <see cref="MsSqlConnection"/> is for the connection domain.
/// Connection type stays invisible above the vault: consumers only see a narrow per-domain interface.
/// </summary>
/// <remarks>
/// Never interpolates values into SQL, never logs SQL-with-values, parameters, or secret material.
/// Each operation opens a pooled <see cref="SqlConnection"/> from the resolved vault connection and
/// disposes it immediately.
/// </remarks>
public abstract class MsSqlDataVaultBase : DataVaultBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlDataVaultBase"/> class with its
    /// already-resolved connection and pepper. The provider resolves both ONCE in system context and
    /// hands them here — there is no async initialization.
    /// </summary>
    /// <param name="vaultName">The vault's name.</param>
    /// <param name="connection">The resolved data connection the vault rides.</param>
    /// <param name="pepper">The resolved pepper (HMAC key) bytes; ownership transfers to the vault.</param>
    /// <param name="logger">Optional logger.</param>
    protected MsSqlDataVaultBase(
        string vaultName,
        [ServiceOptionDependency] IDataConnection connection,
        byte[] pepper,
        ILogger<MsSqlDataVaultBase>? logger)
        : base(vaultName, connection, pepper, logger)
    {
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<T>> Query<T>(
        string sql, CancellationToken cancellationToken, params (string name, object? value)[] parameters)
    {
        // Why: the connection was resolved once in DefaultDataVaultProvider's cache-factory and set
        // at construction; RequireConnection() returns it directly.
        var connectionResult = RequireConnection();
        if (!connectionResult.IsSuccess)
            return connectionResult.ToNewResult<T>();

        if (connectionResult.Value is not IPooledConnectionSource msSqlConnection)
            return GenericResult<T>.Failure(MsSqlDataVaultLog.ConnectionNotMsSql(Logger, Name));

        var openResult = await OpenConnection(msSqlConnection, cancellationToken).ConfigureAwait(false);
        if (!openResult.IsSuccess || openResult.Value is null)
            return openResult.ToNewResult<T>();

        var sqlConnection = openResult.Value;
        await using (sqlConnection.ConfigureAwait(false))
        {
            var command = new SqlCommand(sql, sqlConnection);
            await using (command.ConfigureAwait(false))
            {
                AddParameters(command, parameters);
                try
                {
                    var scalar = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                    return GenericResult<T>.Success(ConvertScalar<T>(scalar));
                }
                catch (SqlException ex)
                {
                    return GenericResult<T>.Failure(MsSqlDataVaultLog.QueryFailed(Logger, ex, Name));
                }
            }
        }
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<int>> NonQuery(
        string sql, CancellationToken cancellationToken, params (string name, object? value)[] parameters)
    {
        // Why: the connection was resolved once in DefaultDataVaultProvider's cache-factory and set
        // at construction; RequireConnection() returns it directly.
        var connectionResult = RequireConnection();
        if (!connectionResult.IsSuccess)
            return connectionResult.ToNewResult<int>();

        if (connectionResult.Value is not IPooledConnectionSource msSqlConnection)
            return GenericResult<int>.Failure(MsSqlDataVaultLog.ConnectionNotMsSql(Logger, Name));

        var openResult = await OpenConnection(msSqlConnection, cancellationToken).ConfigureAwait(false);
        if (!openResult.IsSuccess || openResult.Value is null)
            return openResult.ToNewResult<int>();

        var sqlConnection = openResult.Value;
        await using (sqlConnection.ConfigureAwait(false))
        {
            var command = new SqlCommand(sql, sqlConnection);
            await using (command.ConfigureAwait(false))
            {
                AddParameters(command, parameters);
                try
                {
                    var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    return GenericResult<int>.Success(rowsAffected);
                }
                catch (SqlException ex)
                {
                    return GenericResult<int>.Failure(MsSqlDataVaultLog.NonQueryFailed(Logger, ex, Name));
                }
            }
        }
    }

    /// <summary>
    /// Runs a parameterized read and projects every row via <paramref name="map"/> into a list. This
    /// is the multi-row/multi-column read primitive for vault verbs that list or look up metadata
    /// records (e.g. tokens, agent keys); a single secret compare uses the scalar <see cref="Query{T}"/>
    /// instead. Each row is exposed to <paramref name="map"/> as a case-insensitive column-name → value
    /// map with <c>DBNull</c> normalised to <c>null</c>. Parameterized only; never interpolate values;
    /// never log secret material.
    /// </summary>
    /// <typeparam name="T">The projected row type.</typeparam>
    /// <param name="sql">Parameterized SQL with named parameters only.</param>
    /// <param name="map">Projects one row (column-name → value) to <typeparamref name="T"/>.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    /// <param name="parameters">Named parameter name/value pairs bound as DB parameters.</param>
    // Why: additive to the MsSql vault tier only — DataVaultBase (the connection-agnostic core) and the
    // scalar primitives are unchanged. The reader maps to a plain dictionary so concrete vaults never
    // touch ADO types; connection type stays invisible above the vault.
    protected async Task<IGenericResult<IReadOnlyList<T>>> QueryRows<T>(
        string sql,
        Func<IReadOnlyDictionary<string, object?>, T> map,
        CancellationToken cancellationToken,
        params (string name, object? value)[] parameters)
    {
        if (map is null)
            throw new ArgumentNullException(nameof(map));

        var connectionResult = RequireConnection();
        if (!connectionResult.IsSuccess)
            return connectionResult.ToNewResult<IReadOnlyList<T>>();

        if (connectionResult.Value is not IPooledConnectionSource msSqlConnection)
            return GenericResult<IReadOnlyList<T>>.Failure(MsSqlDataVaultLog.ConnectionNotMsSql(Logger, Name));

        var openResult = await OpenConnection(msSqlConnection, cancellationToken).ConfigureAwait(false);
        if (!openResult.IsSuccess || openResult.Value is null)
            return openResult.ToNewResult<IReadOnlyList<T>>();

        var sqlConnection = openResult.Value;
        await using (sqlConnection.ConfigureAwait(false))
        {
            var command = new SqlCommand(sql, sqlConnection);
            await using (command.ConfigureAwait(false))
            {
                AddParameters(command, parameters);
                try
                {
                    var rows = new List<T>();
                    var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                    await using (reader.ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            var row = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
                            for (var i = 0; i < reader.FieldCount; i++)
                            {
                                var value = reader.GetValue(i);
                                row[reader.GetName(i)] = value is DBNull ? null : value;
                            }

                            rows.Add(map(row));
                        }
                    }

                    return GenericResult<IReadOnlyList<T>>.Success(rows);
                }
                catch (SqlException ex)
                {
                    return GenericResult<IReadOnlyList<T>>.Failure(MsSqlDataVaultLog.QueryFailed(Logger, ex, Name));
                }
            }
        }
    }

    // Why: the vault opens its OWN pooled SqlConnection from the resolved MsSql connection's
    // internal connection-string factory. The shared MsSqlConnection.GetOpenSqlConnection seam was
    // removed; the vault's connection is resolved once in system context (no authenticated user), so
    // the per-user SESSION_CONTEXT that seam set was already a no-op here — RLS user-scoping does not
    // apply to the system-context vault login. Caller MUST dispose the returned connection.
    private async Task<IGenericResult<SqlConnection>> OpenConnection(
        IPooledConnectionSource connection, CancellationToken cancellationToken)
    {
        try
        {
            // Why: the seam returns DbConnection so the abstraction carries no driver dependency;
            // this vault is MsSql-specific ADO, so anything else here is a wiring defect.
            if (connection.CreatePooledConnection() is not SqlConnection sqlConnection)
                return GenericResult<SqlConnection>.Failure(MsSqlDataVaultLog.ConnectionNotMsSql(Logger, Name));
            await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return GenericResult<SqlConnection>.Success(sqlConnection);
        }
        catch (SqlException ex)
        {
            return GenericResult<SqlConnection>.Failure(MsSqlDataVaultLog.QueryFailed(Logger, ex, Name));
        }
    }

    // Why: named parameters bound as SqlParameter — the ONLY way values enter SQL. A null value is
    // bound as SQL NULL (DBNull) because that is the actual value being passed, not a fallback for
    // a missing input.
    private static void AddParameters(SqlCommand command, (string name, object? value)[] parameters)
    {
        foreach (var (name, value) in parameters)
            command.Parameters.Add(new SqlParameter(name, value is null ? DBNull.Value : value));
    }

    // Why: a scalar read returns the first column of the first row, or default when there is no row.
    // A null/DBNull scalar maps to default(T) (e.g. null for byte[]) — a legitimate "no value on file"
    // result the caller inspects (e.g. the anti-enumeration negative path), not a papered-over default.
    private static T ConvertScalar<T>(object? value)
    {
        if (value is null || value is DBNull)
            return default!;
        if (value is T typed)
            return typed;
        return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
    }
}
