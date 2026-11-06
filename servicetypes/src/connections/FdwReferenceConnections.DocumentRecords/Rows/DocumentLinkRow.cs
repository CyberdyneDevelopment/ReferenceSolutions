using System;
using Fdw.Documents.Records.Abstractions.Links;
using Fdw.Results;
using Microsoft.Data.SqlClient;

namespace FdwReferenceConnections.DocumentRecords.Rows;

/// <summary>A row of doc.DocumentLink that is not removed (FDW-3731).</summary>
internal sealed class DocumentLinkRow : IDocumentLink
{
    /// <summary>Initializes a new instance of the <see cref="DocumentLinkRow"/> class.</summary>
    /// <param name="id">The Id.</param>
    /// <param name="organizationId">The OrganizationId.</param>
    /// <param name="documentId">The DocumentId.</param>
    /// <param name="linkKind">The LinkKind.</param>
    /// <param name="targetKind">The TargetKind.</param>
    /// <param name="targetId">The TargetId.</param>
    internal DocumentLinkRow(Guid id, Guid organizationId, Guid documentId, string linkKind, string targetKind, Guid targetId)
    {
        Id = id;
        OrganizationId = organizationId;
        DocumentId = documentId;
        LinkKind = linkKind;
        TargetKind = targetKind;
        TargetId = targetId;
    }

    /// <inheritdoc/>
    public Guid Id { get; }

    /// <inheritdoc/>
    public Guid OrganizationId { get; }

    /// <inheritdoc/>
    public Guid DocumentId { get; }

    /// <inheritdoc/>
    public string LinkKind { get; }

    /// <inheritdoc/>
    public string TargetKind { get; }

    /// <inheritdoc/>
    public Guid TargetId { get; }

    /// <summary>Reads the row the reader is on.</summary>
    /// <param name="reader">The reader, on a row of the statement that selects the columns by name.</param>
    /// <returns>The row.</returns>
    internal static IGenericResult<IDocumentLink> From(SqlDataReader reader) =>
        GenericResult<IDocumentLink>.Success(new DocumentLinkRow(
            reader.GetGuid(reader.GetOrdinal("Id")),
            reader.GetGuid(reader.GetOrdinal("OrganizationId")),
            reader.GetGuid(reader.GetOrdinal("DocumentId")),
            reader.GetString(reader.GetOrdinal("LinkKind")),
            reader.GetString(reader.GetOrdinal("TargetKind")),
            reader.GetGuid(reader.GetOrdinal("TargetId"))));
}
