using System;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Abstractions.Logging;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.MsSql;

/// <summary>
/// The bulk route of a SQL Server command (FDW-1152): the decision to take it, shared by the connection and its unit of work so
/// they route alike.
/// </summary>
internal static class MsSqlBulkRoute
{
    private const string BulkInsert = "BulkInsert";

    // Asked first: a command that would not take the bulk route even if no row could run serially has no use for it, and its
    // container is not sized. Every data command answers; only a bulk insert with rows answers yes.
    private const int NoSerialRows = 0;

    /// <summary>
    /// Says whether the command takes the bulk route into <paramref name="container"/> on a connection that lets an insert run
    /// serially up to <paramref name="maxSerialInsert"/> rows (<see cref="SerialLimit"/>).
    /// </summary>
    internal static bool Takes(IDataCommand command, IStorageContainer container, int maxSerialInsert)
        => command is IDatabaseConnectionCommand routed
           && routed.ReroutesToBulk(NoSerialRows)
           && routed.ReroutesToBulk(SerialLimit(container, maxSerialInsert));

    /// <summary>
    /// The most rows an insert into <paramref name="container"/> runs serially: the connection's configured
    /// <paramref name="maxSerialInsert"/>, or fewer when one SQL Server request cannot carry that many rows of this container's
    /// columns (2100 parameters). Rows beyond it go to the bulk copy, which has no such limit (FDW-1173).
    /// </summary>
    internal static int SerialLimit(IStorageContainer container, int maxSerialInsert)
        => Math.Min(maxSerialInsert, MsSqlBatchInsertTranslator.RowsPerRequest(container));

    /// <summary>
    /// Plans the bulk copy of a command that takes the bulk route, or says why it cannot: this connection's bulk mode copies bulk
    /// inserts only, and the failure is a coded result naming the command and the connection.
    /// </summary>
    internal static IGenericResult<MsSqlBulkCopyPlan> Plan(
        IDataCommand command, IStorageContainer container, ILogger logger, string connectionName, int maxSerialInsert, bool inTransaction)
    {
        if (!string.Equals(command.CommandType, BulkInsert, StringComparison.Ordinal))
        {
            return GenericResult<MsSqlBulkCopyPlan>.Failure(DatabaseCommandLog.BulkRouteUnavailable(
                logger, command.CommandType, connectionName, "the SQL Server bulk mode copies bulk inserts only"));
        }

        DatabaseCommandLog.BulkRouteChosen(
            logger, command.CommandType, connectionName, SerialLimit(container, maxSerialInsert), inTransaction ? " in a unit of work" : string.Empty);
        return MsSqlBulkInsertTranslator.Plan(command, container);
    }
}
