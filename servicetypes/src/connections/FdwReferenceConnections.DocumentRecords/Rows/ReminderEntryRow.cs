using System;
using Fdw.Documents.Records.Abstractions;
using Fdw.Results;
using Microsoft.Data.SqlClient;

namespace FdwReferenceConnections.DocumentRecords.Rows;

/// <summary>A row of doc.ReminderEntry (FDW-3731).</summary>
internal sealed class ReminderEntryRow : IReminderEntry
{
    /// <summary>Initializes a new instance of the <see cref="ReminderEntryRow"/> class.</summary>
    /// <param name="id">The Id.</param>
    /// <param name="requirementId">The RequirementId.</param>
    /// <param name="sentAt">The SentAt.</param>
    /// <param name="recipient">The Recipient.</param>
    internal ReminderEntryRow(Guid id, Guid requirementId, DateTimeOffset sentAt, string recipient)
    {
        Id = id;
        RequirementId = requirementId;
        SentAt = sentAt;
        Recipient = recipient;
    }

    /// <inheritdoc/>
    public Guid Id { get; }

    /// <inheritdoc/>
    public Guid RequirementId { get; }

    /// <inheritdoc/>
    public DateTimeOffset SentAt { get; }

    /// <inheritdoc/>
    public string Recipient { get; }

    /// <summary>Reads the row the reader is on.</summary>
    /// <param name="reader">The reader, on a row of the statement that selects the columns by name.</param>
    /// <returns>The row.</returns>
    internal static IGenericResult<IReminderEntry> From(SqlDataReader reader) =>
        GenericResult<IReminderEntry>.Success(new ReminderEntryRow(
            reader.GetGuid(reader.GetOrdinal("Id")),
            reader.GetGuid(reader.GetOrdinal("RequirementId")),
            reader.GetDateTimeOffset(reader.GetOrdinal("SentAt")),
            reader.GetString(reader.GetOrdinal("Recipient"))));
}
