using System;
using Fdw.Documents.Records.Abstractions;
using Fdw.Results;
using Microsoft.Data.SqlClient;

namespace FdwReferenceConnections.DocumentRecords.Rows;

/// <summary>A row of doc.DocumentRecord (FDW-3731).</summary>
internal sealed class DocumentRecordRow : IDocumentRecord
{
    /// <summary>Initializes a new instance of the <see cref="DocumentRecordRow"/> class.</summary>
    /// <param name="id">The Id.</param>
    /// <param name="organizationId">The OrganizationId.</param>
    /// <param name="documentType">The DocumentType.</param>
    /// <param name="title">The Title.</param>
    /// <param name="state">The State.</param>
    /// <param name="effectiveDate">The EffectiveDate.</param>
    /// <param name="uploadedAt">The UploadedAt.</param>
    /// <param name="recordId">The RecordId.</param>
    internal DocumentRecordRow(Guid id, Guid organizationId, string documentType, string title, string state, DateTimeOffset? effectiveDate, DateTimeOffset uploadedAt, Guid? recordId)
    {
        Id = id;
        OrganizationId = organizationId;
        DocumentType = documentType;
        Title = title;
        State = state;
        EffectiveDate = effectiveDate;
        UploadedAt = uploadedAt;
        RecordId = recordId;
    }

    /// <inheritdoc/>
    public Guid Id { get; }

    /// <inheritdoc/>
    public Guid OrganizationId { get; }

    /// <inheritdoc/>
    public string DocumentType { get; }

    /// <inheritdoc/>
    public string Title { get; }

    /// <inheritdoc/>
    public string State { get; }

    /// <inheritdoc/>
    public DateTimeOffset? EffectiveDate { get; }

    /// <inheritdoc/>
    public DateTimeOffset UploadedAt { get; }

    /// <inheritdoc/>
    public Guid? RecordId { get; }

    /// <summary>Reads the row the reader is on.</summary>
    /// <param name="reader">The reader, on a row of the statement that selects the columns by name.</param>
    /// <returns>The row.</returns>
    internal static IGenericResult<IDocumentRecord> From(SqlDataReader reader) =>
        GenericResult<IDocumentRecord>.Success(new DocumentRecordRow(
            reader.GetGuid(reader.GetOrdinal("Id")),
            reader.GetGuid(reader.GetOrdinal("OrganizationId")),
            reader.GetString(reader.GetOrdinal("DocumentType")),
            reader.GetString(reader.GetOrdinal("Title")),
            reader.GetString(reader.GetOrdinal("State")),
            reader.IsDBNull(reader.GetOrdinal("EffectiveDate")) ? null : reader.GetDateTimeOffset(reader.GetOrdinal("EffectiveDate")),
            reader.GetDateTimeOffset(reader.GetOrdinal("UploadedAt")),
            reader.IsDBNull(reader.GetOrdinal("RecordId")) ? null : reader.GetGuid(reader.GetOrdinal("RecordId"))));
}
