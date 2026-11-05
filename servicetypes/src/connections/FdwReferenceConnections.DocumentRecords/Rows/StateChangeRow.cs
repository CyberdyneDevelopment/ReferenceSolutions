using System;
using Fdw.Documents.Records.Abstractions;
using Fdw.Results;
using Microsoft.Data.SqlClient;

namespace FdwReferenceConnections.DocumentRecords.Rows;

/// <summary>A row of doc.StateChange (FDW-3731).</summary>
internal sealed class StateChangeRow : IStateChange
{
    /// <summary>Initializes a new instance of the <see cref="StateChangeRow"/> class.</summary>
    /// <param name="id">The Id.</param>
    /// <param name="documentId">The DocumentId.</param>
    /// <param name="state">The State.</param>
    /// <param name="enteredAt">The EnteredAt.</param>
    /// <param name="enteredBy">The EnteredBy.</param>
    /// <param name="comment">The Comment.</param>
    internal StateChangeRow(Guid id, Guid documentId, string state, DateTimeOffset enteredAt, string enteredBy, string? comment)
    {
        Id = id;
        DocumentId = documentId;
        State = state;
        EnteredAt = enteredAt;
        EnteredBy = enteredBy;
        Comment = comment;
    }

    /// <inheritdoc/>
    public Guid Id { get; }

    /// <inheritdoc/>
    public Guid DocumentId { get; }

    /// <inheritdoc/>
    public string State { get; }

    /// <inheritdoc/>
    public DateTimeOffset EnteredAt { get; }

    /// <inheritdoc/>
    public string EnteredBy { get; }

    /// <inheritdoc/>
    public string? Comment { get; }

    /// <summary>Reads the row the reader is on.</summary>
    /// <param name="reader">The reader, on a row of the statement that selects the columns by name.</param>
    /// <returns>The row.</returns>
    internal static IGenericResult<IStateChange> From(SqlDataReader reader) =>
        GenericResult<IStateChange>.Success(new StateChangeRow(
            reader.GetGuid(reader.GetOrdinal("Id")),
            reader.GetGuid(reader.GetOrdinal("DocumentId")),
            reader.GetString(reader.GetOrdinal("State")),
            reader.GetDateTimeOffset(reader.GetOrdinal("EnteredAt")),
            reader.GetString(reader.GetOrdinal("EnteredBy")),
            reader.IsDBNull(reader.GetOrdinal("Comment")) ? null : reader.GetString(reader.GetOrdinal("Comment"))));
}
