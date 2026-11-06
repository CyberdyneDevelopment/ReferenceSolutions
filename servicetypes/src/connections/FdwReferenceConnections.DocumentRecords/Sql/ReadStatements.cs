using System;
using Fdw.Documents.Records.Abstractions.Links;

namespace FdwReferenceConnections.DocumentRecords.Sql;

/// <summary>
/// The reads of an organization's document records over the doc tables (FDW-3731). Every read filters by the organization; a read that lists
/// what belongs to one entity first answers whether the organization has that entity, so an unknown id is refused rather than read as empty.
/// </summary>
internal static class ReadStatements
{
    private const string DocumentColumns = "Id, OrganizationId, DocumentType, Title, State, EffectiveDate, UploadedAt, RecordId";
    private const string NodeColumns = "Id, OrganizationId, Kind, ParentId, Name";
    private const string LinkColumns = "Id, OrganizationId, DocumentId, LinkKind, TargetKind, TargetId";

    private const string DocumentFound =
        "SELECT CASE WHEN EXISTS (SELECT 1 FROM doc.DocumentRecord WHERE OrganizationId = @OrganizationId AND Id = @DocumentId) THEN 1 ELSE 0 END;";

    private const string NodeFound =
        "SELECT CASE WHEN EXISTS (SELECT 1 FROM doc.RecordNode WHERE OrganizationId = @OrganizationId AND Id = @RecordId) THEN 1 ELSE 0 END;";

    /// <summary>Reads one document.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="id">The document.</param>
    /// <returns>The statement.</returns>
    public static RecordStatement Document(Guid organizationId, Guid id) => RecordStatement.Of(
        "GetDocumentRecord",
        $"SELECT {DocumentColumns} FROM doc.DocumentRecord WHERE OrganizationId = @OrganizationId AND Id = @Id;",
        RecordStatement.Id("@OrganizationId", organizationId),
        RecordStatement.Id("@Id", id));

    /// <summary>Lists the organization's documents of one type, oldest upload first.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="documentType">The document type.</param>
    /// <returns>The statement.</returns>
    public static RecordStatement Documents(Guid organizationId, string documentType) => RecordStatement.Of(
        "ListDocumentRecords",
        $"SELECT {DocumentColumns} FROM doc.DocumentRecord WHERE OrganizationId = @OrganizationId AND DocumentType = @DocumentType ORDER BY UploadedAt, RowId;",
        RecordStatement.Id("@OrganizationId", organizationId),
        RecordStatement.Value("@DocumentType", documentType));

    /// <summary>Lists a document's files.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="documentId">The document.</param>
    /// <returns>The statement.</returns>
    public static RecordStatement Media(Guid organizationId, Guid documentId) => RecordStatement.Finding(
        "ListStoredMedia",
        "Document",
        documentId,
        DocumentFound + """
         SELECT Id, DocumentId, FileType, Length, ContentHash, Purpose, Path, StoredUnder
           FROM doc.StoredMedia
          WHERE OrganizationId = @OrganizationId AND DocumentId = @DocumentId
          ORDER BY RowId;
        """,
        RecordStatement.Id("@OrganizationId", organizationId),
        RecordStatement.Id("@DocumentId", documentId));

    /// <summary>Reads one record node.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="id">The node.</param>
    /// <returns>The statement.</returns>
    public static RecordStatement Node(Guid organizationId, Guid id) => RecordStatement.Of(
        "GetRecordNode",
        $"SELECT {NodeColumns} FROM doc.RecordNode WHERE OrganizationId = @OrganizationId AND Id = @Id;",
        RecordStatement.Id("@OrganizationId", organizationId),
        RecordStatement.Id("@Id", id));

    /// <summary>Lists the nodes at the top of the organization's record tree.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <returns>The statement.</returns>
    public static RecordStatement TopNodes(Guid organizationId) => RecordStatement.Of(
        "ListRecordChildren",
        $"SELECT {NodeColumns} FROM doc.RecordNode WHERE OrganizationId = @OrganizationId AND ParentId IS NULL ORDER BY Name, RowId;",
        RecordStatement.Id("@OrganizationId", organizationId));

    /// <summary>Lists the children of a record node.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="parentId">The node.</param>
    /// <returns>The statement.</returns>
    public static RecordStatement Children(Guid organizationId, Guid parentId) => RecordStatement.Finding(
        "ListRecordChildren",
        "Record",
        parentId,
        NodeFound + $" SELECT {NodeColumns} FROM doc.RecordNode WHERE OrganizationId = @OrganizationId AND ParentId = @RecordId ORDER BY Name, RowId;",
        RecordStatement.Id("@OrganizationId", organizationId),
        RecordStatement.Id("@RecordId", parentId));

    /// <summary>Lists the attribute values of a document or a record node.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="ownerId">The document or node.</param>
    /// <returns>The statement.</returns>
    public static RecordStatement AttributeValues(Guid organizationId, Guid ownerId) => RecordStatement.Finding(
        "ListAttributeValues",
        "Owner",
        ownerId,
        """
        SELECT CASE WHEN EXISTS (SELECT 1 FROM doc.DocumentRecord WHERE OrganizationId = @OrganizationId AND Id = @OwnerId)
                      OR EXISTS (SELECT 1 FROM doc.RecordNode WHERE OrganizationId = @OrganizationId AND Id = @OwnerId)
                    THEN 1 ELSE 0 END;
         SELECT Id, OwnerId, AttributeCode, Text, Number, Date, Flag
           FROM doc.AttributeValue
          WHERE OrganizationId = @OrganizationId AND OwnerId = @OwnerId
          ORDER BY AttributeCode;
        """,
        RecordStatement.Id("@OrganizationId", organizationId),
        RecordStatement.Id("@OwnerId", ownerId));

    /// <summary>Lists a document's state changes, oldest first.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="documentId">The document.</param>
    /// <returns>The statement.</returns>
    public static RecordStatement StateChanges(Guid organizationId, Guid documentId) => RecordStatement.Finding(
        "ListStateChanges",
        "Document",
        documentId,
        DocumentFound + """
         SELECT Id, DocumentId, State, EnteredAt, EnteredBy, Comment
           FROM doc.StateChange
          WHERE OrganizationId = @OrganizationId AND DocumentId = @DocumentId
          ORDER BY EnteredAt, RowId;
        """,
        RecordStatement.Id("@OrganizationId", organizationId),
        RecordStatement.Id("@DocumentId", documentId));

    /// <summary>Lists a record node's requirements.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="recordId">The node.</param>
    /// <returns>The statement.</returns>
    public static RecordStatement Requirements(Guid organizationId, Guid recordId) => RecordStatement.Finding(
        "ListRecordRequirements",
        "Record",
        recordId,
        NodeFound + """
         SELECT Id, RecordId, ExpectedDocumentType, SatisfiedBy
           FROM doc.RecordRequirement
          WHERE OrganizationId = @OrganizationId AND RecordId = @RecordId
          ORDER BY RowId;
        """,
        RecordStatement.Id("@OrganizationId", organizationId),
        RecordStatement.Id("@RecordId", recordId));

    /// <summary>Lists the reminders sent about a requirement, oldest first.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="requirementId">The requirement.</param>
    /// <returns>The statement.</returns>
    public static RecordStatement Reminders(Guid organizationId, Guid requirementId) => RecordStatement.Finding(
        "ListReminderEntries",
        "Requirement",
        requirementId,
        """
        SELECT CASE WHEN EXISTS (SELECT 1 FROM doc.RecordRequirement WHERE OrganizationId = @OrganizationId AND Id = @RequirementId) THEN 1 ELSE 0 END;
         SELECT Id, RequirementId, SentAt, Recipient
           FROM doc.ReminderEntry
          WHERE OrganizationId = @OrganizationId AND RequirementId = @RequirementId
          ORDER BY SentAt, RowId;
        """,
        RecordStatement.Id("@OrganizationId", organizationId),
        RecordStatement.Id("@RequirementId", requirementId));

    /// <summary>Reads the organization's revision.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <returns>The statement.</returns>
    public static RecordStatement Revision(Guid organizationId) => RecordStatement.Of(
        "GetOrganizationRevision",
        "SELECT OrganizationId, Revision FROM doc.OrganizationRevision WHERE OrganizationId = @OrganizationId;",
        RecordStatement.Id("@OrganizationId", organizationId));

    /// <summary>Lists the links that start from a document and are not removed.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="documentId">The document.</param>
    /// <returns>The statement.</returns>
    public static RecordStatement LinksFrom(Guid organizationId, Guid documentId) => RecordStatement.Finding(
        "ListLinksFrom",
        "Document",
        documentId,
        DocumentFound + $" SELECT {LinkColumns} FROM doc.DocumentLink WHERE OrganizationId = @OrganizationId AND DocumentId = @DocumentId AND RemovedAt IS NULL ORDER BY RowId;",
        RecordStatement.Id("@OrganizationId", organizationId),
        RecordStatement.Id("@DocumentId", documentId));

    /// <summary>Lists the links that point at an entity and are not removed.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="targetKind">The entity's kind.</param>
    /// <param name="targetId">The entity.</param>
    /// <returns>The statement.</returns>
    public static RecordStatement LinksTo(Guid organizationId, string targetKind, Guid targetId) => RecordStatement.Of(
        "ListLinksTo",
        $"SELECT {LinkColumns} FROM doc.DocumentLink WHERE OrganizationId = @OrganizationId AND TargetKind = @TargetKind AND TargetId = @TargetId AND RemovedAt IS NULL ORDER BY RowId;",
        RecordStatement.Id("@OrganizationId", organizationId),
        RecordStatement.Value("@TargetKind", targetKind),
        RecordStatement.Id("@TargetId", targetId));

    /// <summary>Answers whether a link target of a kind this connection checks is the organization's.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="targetKind">The target's kind: <see cref="LinkTargetKinds.Document"/> or <see cref="LinkTargetKinds.Record"/>.</param>
    /// <param name="targetId">The target.</param>
    /// <returns>The statement, or null for a kind this connection does not check.</returns>
    public static RecordStatement? TargetFound(Guid organizationId, string targetKind, Guid targetId)
    {
        var table = string.Equals(targetKind, LinkTargetKinds.Document, StringComparison.OrdinalIgnoreCase) ? "doc.DocumentRecord"
            : string.Equals(targetKind, LinkTargetKinds.Record, StringComparison.OrdinalIgnoreCase) ? "doc.RecordNode"
            : null;
        return table is null
            ? null
            : RecordStatement.Of(
                "LinkTargetExists",
                $"SELECT CASE WHEN EXISTS (SELECT 1 FROM {table} WHERE OrganizationId = @OrganizationId AND Id = @TargetId) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;",
                RecordStatement.Id("@OrganizationId", organizationId),
                RecordStatement.Id("@TargetId", targetId));
    }
}
