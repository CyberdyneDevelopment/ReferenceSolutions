using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.MsSql.Logging;
using Microsoft.Data.SqlClient;

namespace FdwReferenceConnections.MsSql;

/// <summary>
/// SQL Server says which generation of its database a connection reaches (FDW-3207): the database's id and the time it
/// was created, which change when the database is dropped and created again.
/// </summary>
public sealed partial class MsSqlConnection : IDatabaseGenerationConnection
{
    // Why sys.databases and DB_ID(): every login sees the row of the database it is connected to, so this needs no
    // permission beyond the connection's own, and create_date is set by CREATE DATABASE, so a drop and create (the seed
    // pipeline, sqlpackage, a script) changes it. The id is read with it because a restore over the same name can keep a
    // creation time while taking a new id.
    private const string DatabaseGenerationQuery = "SELECT database_id, create_date FROM sys.databases WHERE database_id = DB_ID()";

    /// <inheritdoc />
    public async Task<IGenericResult<string>> DatabaseGeneration(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var sqlConnection = CreatePooledConnection();
            await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var sqlCommand = new SqlCommand(DatabaseGenerationQuery, sqlConnection) { CommandTimeout = _commandTimeoutSeconds };
            await using var reader = await sqlCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
                ? GenericResult<string>.Success(
                    string.Create(CultureInfo.InvariantCulture, $"{reader.GetInt32(0)}@{reader.GetDateTime(1):O}"))
                : GenericResult<string>.Failure(
                    MsSqlConnectionLogger.SqlExecutionError(Logger, DatabaseGenerationQuery, "the database has no row in sys.databases", 0));
        }
        catch (SqlException ex)
        {
            return GenericResult<string>.Failure(
                MsSqlConnectionLogger.SqlExecutionError(Logger, DatabaseGenerationQuery, ex.Message, ex.Number));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult<string>.Failure(
                MsSqlConnectionLogger.SqlExecutionFailedWithMessage(Logger, ex, DatabaseGenerationQuery));
        }
    }
}
