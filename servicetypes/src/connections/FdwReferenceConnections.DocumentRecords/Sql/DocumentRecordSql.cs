using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Documents.Records.Abstractions.Results;
using Fdw.Results;
using Fdw.Services.Connections.MsSql.Logging;
using FdwReferenceConnections.DocumentRecords.Logging;
using FdwReferenceConnections.MsSql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.DocumentRecords.Sql;

/// <summary>
/// Runs the DocumentRecord connection's statements on the SQL Server connection it wraps (FDW-3731), the way the script extension does
/// (FDW-1447): on one of its pooled connections, with its command timeout and the caller's session context. A read places the context in
/// its own batch; a write sets it once and runs every statement and the revision bump in one transaction, as
/// <see cref="MsSqlConnection.BeginTransaction"/> does, so a write and its revision land together or not at all.
/// </summary>
internal sealed class DocumentRecordSql
{
    private const string RaiseRevision = """
        DECLARE @Raised BIGINT;
        UPDATE doc.OrganizationRevision
           SET @Raised = Revision = Revision + 1, ModifyDate = sysdatetimeoffset(), ModifyBy = suser_sname()
         WHERE OrganizationId = @OrganizationId AND Revision = @Expected;
        SELECT @Raised AS Raised, (SELECT Revision FROM doc.OrganizationRevision WHERE OrganizationId = @OrganizationId) AS Stored;
        """;

    private readonly MsSqlConnection _connection;

    /// <summary>Initializes a new instance of the <see cref="DocumentRecordSql"/> class.</summary>
    /// <param name="connection">The SQL Server connection the statements run on.</param>
    internal DocumentRecordSql(MsSqlConnection connection)
    {
        _connection = connection;
    }

    private ILogger Logger => _connection.ScriptLogger;

    /// <summary>Answers the rows of a read, refusing it when the entity it reads for is not the organization's.</summary>
    /// <typeparam name="TItem">What a row answers.</typeparam>
    /// <param name="organizationId">The organization.</param>
    /// <param name="statement">The read.</param>
    /// <param name="map">Reads one row.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The rows, or the refusal.</returns>
    internal async Task<IGenericResult<IReadOnlyList<TItem>>> Rows<TItem>(
        Guid organizationId,
        RecordStatement statement,
        Func<SqlDataReader, IGenericResult<TItem>> map,
        CancellationToken cancellationToken)
    {
        using var command = statement.Command();
        var placement = MsSqlConnection.SessionContextPlacement.Separate;
        try
        {
            var sqlConnection = _connection.CreatePooledConnection();
            await using (sqlConnection.ConfigureAwait(false))
            {
                await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                _connection.Attach(command, sqlConnection);

                var placed = await _connection.PlaceSessionContext(sqlConnection, command, cancellationToken).ConfigureAwait(false);
                if (!placed.IsSuccess || placed.Value is not { } placedContext)
                    return placed.ToNewResult<IReadOnlyList<TItem>>();

                placement = placedContext;
                DocumentRecordSqlLog.StatementRun(Logger, statement.Name, organizationId);
                using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                placement.Applied(sqlConnection);
                return await Read(organizationId, statement, reader, map, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (SqlException ex)
        {
            MsSqlConnectionLogger.SqlExecutionError(Logger, statement.Name, ex.Message, ex.Number);
            return GenericResult<IReadOnlyList<TItem>>.Failure(_connection.FailureMessages(command, placement, ex, statement.Name));
        }
        catch (Exception ex)
        {
            MsSqlConnectionLogger.ExecutionException(Logger, statement.Name, ex.Message);
            return GenericResult<IReadOnlyList<TItem>>.Failure(MsSqlConnectionLogger.SqlExecutionFailedWithMessage(Logger, ex, statement.Name));
        }
    }

    /// <summary>Answers the one row a read finds, refusing it when there is none.</summary>
    /// <typeparam name="TItem">What the row answers.</typeparam>
    /// <param name="organizationId">The organization.</param>
    /// <param name="entity">The entity read, as a refusal names it.</param>
    /// <param name="id">The entity's public id.</param>
    /// <param name="statement">The read.</param>
    /// <param name="map">Reads the row.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The row, or the refusal.</returns>
    internal async Task<IGenericResult<TItem>> One<TItem>(
        Guid organizationId,
        string entity,
        Guid id,
        RecordStatement statement,
        Func<SqlDataReader, IGenericResult<TItem>> map,
        CancellationToken cancellationToken)
    {
        var rows = await Rows(organizationId, statement, map, cancellationToken).ConfigureAwait(false);
        if (!rows.IsSuccess || rows.Value is not { } found)
            return rows.ToNewResult<TItem>();

        return found.Count == 0
            ? NotFound<TItem>(organizationId, entity, id)
            : GenericResult<TItem>.Success(found[0]);
    }

    /// <summary>
    /// Runs a write and raises the organization's revision in one transaction, refusing it with nothing written when the revision moved on
    /// since <paramref name="expectedRevision"/>, when the organization's records were never started, or when an entity a statement must
    /// find is not the organization's.
    /// </summary>
    /// <param name="command">The command, for the log.</param>
    /// <param name="organizationId">The organization.</param>
    /// <param name="expectedRevision">The revision the write was made against.</param>
    /// <param name="statements">The write's statements, in order.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The organization's new revision, or the refusal.</returns>
    internal async Task<IGenericResult<long>> Revised(
        string command,
        Guid organizationId,
        long expectedRevision,
        IReadOnlyList<RecordStatement> statements,
        CancellationToken cancellationToken)
    {
        return await InTransaction<long>(command, async (sqlConnection, transaction) =>
        {
            var raised = await Raise(sqlConnection, transaction, organizationId, expectedRevision, cancellationToken).ConfigureAwait(false);
            if (raised.IsFailure)
                return raised;

            var ran = await RunAll(sqlConnection, transaction, organizationId, statements, cancellationToken).ConfigureAwait(false);
            if (ran.IsFailure)
                return ran.ToNewResult<long>();

            DocumentRecordSqlLog.WriteLanded(Logger, command, organizationId, raised.Value);
            return raised;
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Runs statements that change no record in one transaction, answering how many rows the last one wrote.</summary>
    /// <param name="command">The command, for the log.</param>
    /// <param name="organizationId">The organization.</param>
    /// <param name="statements">The statements, in order.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The rows the last statement wrote, or the refusal.</returns>
    internal Task<IGenericResult<int>> Unrevised(
        string command,
        Guid organizationId,
        IReadOnlyList<RecordStatement> statements,
        CancellationToken cancellationToken) =>
        InTransaction<int>(command, (sqlConnection, transaction) =>
            RunAll(sqlConnection, transaction, organizationId, statements, cancellationToken), cancellationToken);

    /// <summary>The refusal of a read or write for an entity the organization does not have.</summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="organizationId">The organization.</param>
    /// <param name="entity">The entity, as the refusal names it.</param>
    /// <param name="id">The entity's public id.</param>
    /// <returns>The failure, coded DocumentRecordNotFound.</returns>
    internal IGenericResult<T> NotFound<T>(Guid organizationId, string entity, Guid id)
    {
        DocumentRecordSqlLog.RecordNotFound(Logger, organizationId, entity, id);
        return GenericResult<T>.Failure(
            DocumentRecordResultCodes.ByName("DocumentRecordNotFound"),
            ResultDetails.Create(
                "OrganizationId", organizationId.ToString("D", CultureInfo.InvariantCulture),
                "Entity", entity,
                "Id", id.ToString("D", CultureInfo.InvariantCulture)));
    }

    /// <summary>The refusal of a write made against a revision the organization's records are no longer at.</summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="organizationId">The organization.</param>
    /// <param name="current">The revision the records are at.</param>
    /// <param name="expected">The revision the write was made against.</param>
    /// <returns>The failure, coded DocumentRevisionConflict.</returns>
    internal IGenericResult<T> Conflict<T>(Guid organizationId, string current, string expected)
    {
        DocumentRecordSqlLog.RevisionConflict(Logger, organizationId, current, expected);
        return GenericResult<T>.Failure(
            DocumentRecordResultCodes.ByName("DocumentRevisionConflict"),
            ResultDetails.Create(
                "OrganizationId", organizationId.ToString("D", CultureInfo.InvariantCulture),
                "Current", current,
                "Expected", expected));
    }

    private async Task<IGenericResult<IReadOnlyList<TItem>>> Read<TItem>(
        Guid organizationId,
        RecordStatement statement,
        SqlDataReader reader,
        Func<SqlDataReader, IGenericResult<TItem>> map,
        CancellationToken cancellationToken)
    {
        if (statement.MustFind is { } entity)
        {
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false) || reader.GetInt32(0) != 1)
                return NotFound<IReadOnlyList<TItem>>(organizationId, entity, statement.MustFindId);

            await reader.NextResultAsync(cancellationToken).ConfigureAwait(false);
        }

        var items = new List<TItem>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var item = map(reader);
            if (!item.IsSuccess || item.Value is not { } value)
                return item.ToNewResult<IReadOnlyList<TItem>>();

            items.Add(value);
        }

        return GenericResult<IReadOnlyList<TItem>>.Success(items);
    }

    // Opens a pooled connection, sets the session context once for the whole unit, and commits the work only when it succeeds. Disposing an
    // uncommitted transaction rolls it back, so a refusal or a throw leaves nothing written.
    private async Task<IGenericResult<T>> InTransaction<T>(
        string command,
        Func<SqlConnection, SqlTransaction, Task<IGenericResult<T>>> work,
        CancellationToken cancellationToken)
    {
        try
        {
            var sqlConnection = _connection.CreatePooledConnection();
            await using (sqlConnection.ConfigureAwait(false))
            {
                await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                var contextSet = await _connection.SetUserSessionContext(sqlConnection, cancellationToken).ConfigureAwait(false);
                if (contextSet.IsFailure)
                    return contextSet.ToNewResult<T>();

                var transaction = (SqlTransaction)await sqlConnection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
                await using (transaction.ConfigureAwait(false))
                {
                    var result = await work(sqlConnection, transaction).ConfigureAwait(false);
                    if (result.IsSuccess)
                        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    else
                        await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);

                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            MsSqlConnectionLogger.ExecutionException(Logger, command, ex.Message);
            return GenericResult<T>.Failure(MsSqlConnectionLogger.SqlExecutionFailedWithMessage(Logger, ex, command));
        }
    }

    private async Task<IGenericResult<long>> Raise(
        SqlConnection sqlConnection,
        SqlTransaction transaction,
        Guid organizationId,
        long expectedRevision,
        CancellationToken cancellationToken)
    {
        var statement = RecordStatement.Of(
            "RaiseRevision",
            RaiseRevision,
            RecordStatement.Id("@OrganizationId", organizationId),
            RecordStatement.Value("@Expected", expectedRevision));
        using var command = statement.Command();
        try
        {
            _connection.Attach(command, sqlConnection);
            command.Transaction = transaction;
            DocumentRecordSqlLog.StatementRun(Logger, statement.Name, organizationId);
            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            if (!reader.IsDBNull(0))
                return GenericResult<long>.Success(reader.GetInt64(0));

            return reader.IsDBNull(1)
                ? NotFound<long>(organizationId, "OrganizationRevision", organizationId)
                : Conflict<long>(
                    organizationId,
                    reader.GetInt64(1).ToString(CultureInfo.InvariantCulture),
                    expectedRevision.ToString(CultureInfo.InvariantCulture));
        }
        catch (SqlException ex)
        {
            MsSqlConnectionLogger.SqlExecutionError(Logger, statement.Name, ex.Message, ex.Number);
            return GenericResult<long>.Failure(
                _connection.FailureMessages(command, MsSqlConnection.SessionContextPlacement.Separate, ex, statement.Name));
        }
    }

    private async Task<IGenericResult<int>> RunAll(
        SqlConnection sqlConnection,
        SqlTransaction transaction,
        Guid organizationId,
        IReadOnlyList<RecordStatement> statements,
        CancellationToken cancellationToken)
    {
        IGenericResult<int> last = GenericResult<int>.Success(0);
        foreach (var statement in statements)
        {
            last = await Run(sqlConnection, transaction, organizationId, statement, cancellationToken).ConfigureAwait(false);
            if (last.IsFailure)
                return last;
        }

        return last;
    }

    // A statement that must find an entity answers 1 or 0; any other answers the rows it wrote.
    private async Task<IGenericResult<int>> Run(
        SqlConnection sqlConnection,
        SqlTransaction transaction,
        Guid organizationId,
        RecordStatement statement,
        CancellationToken cancellationToken)
    {
        using var command = statement.Command();
        try
        {
            _connection.Attach(command, sqlConnection);
            command.Transaction = transaction;
            DocumentRecordSqlLog.StatementRun(Logger, statement.Name, organizationId);
            if (statement.MustFind is not { } entity)
                return GenericResult<int>.Success(await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false));

            return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is 1
                ? GenericResult<int>.Success(1)
                : NotFound<int>(organizationId, entity, statement.MustFindId);
        }
        catch (SqlException ex)
        {
            MsSqlConnectionLogger.SqlExecutionError(Logger, statement.Name, ex.Message, ex.Number);
            return GenericResult<int>.Failure(
                _connection.FailureMessages(command, MsSqlConnection.SessionContextPlacement.Separate, ex, statement.Name));
        }
    }
}
