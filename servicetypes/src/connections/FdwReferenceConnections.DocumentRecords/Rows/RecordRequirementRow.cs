using System;
using Fdw.Documents.Records.Abstractions;
using Fdw.Results;
using Microsoft.Data.SqlClient;

namespace FdwReferenceConnections.DocumentRecords.Rows;

/// <summary>A row of doc.RecordRequirement (FDW-3731).</summary>
internal sealed class RecordRequirementRow : IRecordRequirement
{
    /// <summary>Initializes a new instance of the <see cref="RecordRequirementRow"/> class.</summary>
    /// <param name="id">The Id.</param>
    /// <param name="recordId">The RecordId.</param>
    /// <param name="expectedDocumentType">The ExpectedDocumentType.</param>
    /// <param name="satisfiedBy">The SatisfiedBy.</param>
    internal RecordRequirementRow(Guid id, Guid recordId, string expectedDocumentType, Guid? satisfiedBy)
    {
        Id = id;
        RecordId = recordId;
        ExpectedDocumentType = expectedDocumentType;
        SatisfiedBy = satisfiedBy;
    }

    /// <inheritdoc/>
    public Guid Id { get; }

    /// <inheritdoc/>
    public Guid RecordId { get; }

    /// <inheritdoc/>
    public string ExpectedDocumentType { get; }

    /// <inheritdoc/>
    public Guid? SatisfiedBy { get; }

    /// <summary>Reads the row the reader is on.</summary>
    /// <param name="reader">The reader, on a row of the statement that selects the columns by name.</param>
    /// <returns>The row.</returns>
    internal static IGenericResult<IRecordRequirement> From(SqlDataReader reader) =>
        GenericResult<IRecordRequirement>.Success(new RecordRequirementRow(
            reader.GetGuid(reader.GetOrdinal("Id")),
            reader.GetGuid(reader.GetOrdinal("RecordId")),
            reader.GetString(reader.GetOrdinal("ExpectedDocumentType")),
            reader.IsDBNull(reader.GetOrdinal("SatisfiedBy")) ? null : reader.GetGuid(reader.GetOrdinal("SatisfiedBy"))));
}
