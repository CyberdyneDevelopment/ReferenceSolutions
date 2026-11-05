using System;
using System.Collections.Generic;
using Fdw.Documents.Records.Abstractions;
using Fdw.Documents.Records.Abstractions.Links;

namespace FdwReferenceConnections.DocumentRecords.Sql;

/// <summary>
/// The writes of an organization's document records over the doc tables (FDW-3731). Each answers its statements in order; the connection runs
/// them in one transaction with the revision bump (<see cref="DocumentRecordSql.Revised"/>). A write first finds each entity it hangs off,
/// so an id the organization does not have is refused by name rather than as a foreign key error. Writes always use the command's
/// organization; an id that belongs to another organization matches no row here and is then refused by the unique key on Id.
/// </summary>
internal static class WriteStatements
{
    /// <summary>Saves a document, new or changed.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="document">The document.</param>
    /// <returns>The statements.</returns>
    public static IReadOnlyList<RecordStatement> SaveDocument(Guid organizationId, IDocumentRecord document)
    {
        var statements = new List<RecordStatement>();
        if (document.RecordId is { } recordId)
            statements.Add(NodeFound(organizationId, recordId));

        statements.Add(RecordStatement.Of(
            "SaveDocumentRecord",
            """
            UPDATE doc.DocumentRecord
               SET DocumentType = @DocumentType, Title = @Title, State = @State, EffectiveDate = @EffectiveDate, UploadedAt = @UploadedAt,
                   RecordId = @RecordId, ModifyDate = sysdatetimeoffset(), ModifyBy = suser_sname()
             WHERE OrganizationId = @OrganizationId AND Id = @Id;
            IF @@ROWCOUNT = 0
                INSERT INTO doc.DocumentRecord (Id, OrganizationId, DocumentType, Title, State, EffectiveDate, UploadedAt, RecordId)
                VALUES (@Id, @OrganizationId, @DocumentType, @Title, @State, @EffectiveDate, @UploadedAt, @RecordId);
            """,
            RecordStatement.Id("@OrganizationId", organizationId),
            RecordStatement.Id("@Id", document.Id),
            RecordStatement.Value("@DocumentType", document.DocumentType),
            RecordStatement.Value("@Title", document.Title),
            RecordStatement.Value("@State", document.State),
            RecordStatement.Value("@EffectiveDate", document.EffectiveDate),
            RecordStatement.Value("@UploadedAt", document.UploadedAt),
            RecordStatement.Id("@RecordId", document.RecordId)));
        return statements;
    }

    /// <summary>Adds the record of a file of a document.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="media">The file's record.</param>
    /// <returns>The statements.</returns>
    public static IReadOnlyList<RecordStatement> AddMedia(Guid organizationId, IStoredMedia media) =>
    [
        DocumentFound(organizationId, media.DocumentId),
        RecordStatement.Of(
            "AddStoredMedia",
            """
            INSERT INTO doc.StoredMedia (Id, OrganizationId, DocumentId, FileType, Length, ContentHash, Purpose, Path, StoredUnder)
            VALUES (@Id, @OrganizationId, @DocumentId, @FileType, @Length, @ContentHash, @Purpose, @Path, @StoredUnder);
            """,
            RecordStatement.Id("@OrganizationId", organizationId),
            RecordStatement.Id("@Id", media.Id),
            RecordStatement.Id("@DocumentId", media.DocumentId),
            RecordStatement.Value("@FileType", media.FileType),
            RecordStatement.Value("@Length", media.Length),
            RecordStatement.Value("@ContentHash", media.ContentHash),
            RecordStatement.Value("@Purpose", media.Purpose),
            RecordStatement.Value("@Path", media.Path.Value),
            RecordStatement.Id("@StoredUnder", media.StoredUnder)),
    ];

    /// <summary>Saves a record node, new or changed.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="node">The node.</param>
    /// <returns>The statements.</returns>
    public static IReadOnlyList<RecordStatement> SaveNode(Guid organizationId, IRecordNode node)
    {
        var statements = new List<RecordStatement>();
        if (node.ParentId is { } parentId)
            statements.Add(NodeFound(organizationId, parentId));

        statements.Add(RecordStatement.Of(
            "SaveRecordNode",
            """
            UPDATE doc.RecordNode
               SET Kind = @Kind, ParentId = @ParentId, Name = @Name, ModifyDate = sysdatetimeoffset(), ModifyBy = suser_sname()
             WHERE OrganizationId = @OrganizationId AND Id = @Id;
            IF @@ROWCOUNT = 0
                INSERT INTO doc.RecordNode (Id, OrganizationId, Kind, ParentId, Name)
                VALUES (@Id, @OrganizationId, @Kind, @ParentId, @Name);
            """,
            RecordStatement.Id("@OrganizationId", organizationId),
            RecordStatement.Id("@Id", node.Id),
            RecordStatement.Value("@Kind", node.Kind),
            RecordStatement.Id("@ParentId", node.ParentId),
            RecordStatement.Value("@Name", node.Name)));
        return statements;
    }

    /// <summary>Sets one attribute value, replacing the owner's value of the same attribute code.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="value">The value.</param>
    /// <returns>The statements.</returns>
    public static IReadOnlyList<RecordStatement> SetAttribute(Guid organizationId, IAttributeValue value) =>
    [
        RecordStatement.Finding(
            "FindOwner",
            "Owner",
            value.OwnerId,
            """
            SELECT CASE WHEN EXISTS (SELECT 1 FROM doc.DocumentRecord WHERE OrganizationId = @OrganizationId AND Id = @OwnerId)
                          OR EXISTS (SELECT 1 FROM doc.RecordNode WHERE OrganizationId = @OrganizationId AND Id = @OwnerId)
                        THEN 1 ELSE 0 END;
            """,
            RecordStatement.Id("@OrganizationId", organizationId),
            RecordStatement.Id("@OwnerId", value.OwnerId)),
        RecordStatement.Of(
            "SetAttributeValue",
            """
            UPDATE doc.AttributeValue
               SET Id = @Id, Text = @Text, Number = @Number, Date = @Date, Flag = @Flag, ModifyDate = sysdatetimeoffset(), ModifyBy = suser_sname()
             WHERE OrganizationId = @OrganizationId AND OwnerId = @OwnerId AND AttributeCode = @AttributeCode;
            IF @@ROWCOUNT = 0
                INSERT INTO doc.AttributeValue (Id, OrganizationId, OwnerId, AttributeCode, Text, Number, Date, Flag)
                VALUES (@Id, @OrganizationId, @OwnerId, @AttributeCode, @Text, @Number, @Date, @Flag);
            """,
            RecordStatement.Id("@OrganizationId", organizationId),
            RecordStatement.Id("@Id", value.Id),
            RecordStatement.Id("@OwnerId", value.OwnerId),
            RecordStatement.Value("@AttributeCode", value.AttributeCode),
            RecordStatement.Value("@Text", value.Text),
            RecordStatement.Value("@Number", value.Number),
            RecordStatement.Value("@Date", value.Date),
            RecordStatement.Value("@Flag", value.Flag)),
    ];

    /// <summary>Moves a document into a state: the change is appended and the document takes the state.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="change">The change.</param>
    /// <returns>The statements.</returns>
    public static IReadOnlyList<RecordStatement> EnterState(Guid organizationId, IStateChange change) =>
    [
        DocumentFound(organizationId, change.DocumentId),
        RecordStatement.Of(
            "EnterState",
            """
            INSERT INTO doc.StateChange (Id, OrganizationId, DocumentId, State, EnteredAt, EnteredBy, Comment)
            VALUES (@Id, @OrganizationId, @DocumentId, @State, @EnteredAt, @EnteredBy, @Comment);
            UPDATE doc.DocumentRecord
               SET State = @State, ModifyDate = sysdatetimeoffset(), ModifyBy = suser_sname()
             WHERE OrganizationId = @OrganizationId AND Id = @DocumentId;
            """,
            RecordStatement.Id("@OrganizationId", organizationId),
            RecordStatement.Id("@Id", change.Id),
            RecordStatement.Id("@DocumentId", change.DocumentId),
            RecordStatement.Value("@State", change.State),
            RecordStatement.Value("@EnteredAt", change.EnteredAt),
            RecordStatement.Value("@EnteredBy", change.EnteredBy),
            RecordStatement.Value("@Comment", change.Comment)),
    ];

    /// <summary>Saves a requirement, new or satisfied.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="requirement">The requirement.</param>
    /// <returns>The statements.</returns>
    public static IReadOnlyList<RecordStatement> SaveRequirement(Guid organizationId, IRecordRequirement requirement)
    {
        var statements = new List<RecordStatement> { NodeFound(organizationId, requirement.RecordId) };
        if (requirement.SatisfiedBy is { } satisfiedBy)
            statements.Add(DocumentFound(organizationId, satisfiedBy));

        statements.Add(RecordStatement.Of(
            "SaveRecordRequirement",
            """
            UPDATE doc.RecordRequirement
               SET RecordId = @RecordId, ExpectedDocumentType = @ExpectedDocumentType, SatisfiedBy = @SatisfiedBy,
                   ModifyDate = sysdatetimeoffset(), ModifyBy = suser_sname()
             WHERE OrganizationId = @OrganizationId AND Id = @Id;
            IF @@ROWCOUNT = 0
                INSERT INTO doc.RecordRequirement (Id, OrganizationId, RecordId, ExpectedDocumentType, SatisfiedBy)
                VALUES (@Id, @OrganizationId, @RecordId, @ExpectedDocumentType, @SatisfiedBy);
            """,
            RecordStatement.Id("@OrganizationId", organizationId),
            RecordStatement.Id("@Id", requirement.Id),
            RecordStatement.Id("@RecordId", requirement.RecordId),
            RecordStatement.Value("@ExpectedDocumentType", requirement.ExpectedDocumentType),
            RecordStatement.Id("@SatisfiedBy", requirement.SatisfiedBy)));
        return statements;
    }

    /// <summary>
    /// Records a reminder that was sent. It changes no record, so it takes no revision; recording the same entry again writes nothing, and the
    /// last statement answers whether this one was new.
    /// </summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="entry">The reminder.</param>
    /// <returns>The statements.</returns>
    public static IReadOnlyList<RecordStatement> RecordReminder(Guid organizationId, IReminderEntry entry) =>
    [
        RecordStatement.Finding(
            "FindRequirement",
            "Requirement",
            entry.RequirementId,
            "SELECT CASE WHEN EXISTS (SELECT 1 FROM doc.RecordRequirement WHERE OrganizationId = @OrganizationId AND Id = @RequirementId) THEN 1 ELSE 0 END;",
            RecordStatement.Id("@OrganizationId", organizationId),
            RecordStatement.Id("@RequirementId", entry.RequirementId)),
        RecordStatement.Of(
            "RecordReminder",
            """
            INSERT INTO doc.ReminderEntry (Id, OrganizationId, RequirementId, SentAt, Recipient)
            SELECT @Id, @OrganizationId, @RequirementId, @SentAt, @Recipient
             WHERE NOT EXISTS (SELECT 1 FROM doc.ReminderEntry WHERE Id = @Id);
            """,
            RecordStatement.Id("@OrganizationId", organizationId),
            RecordStatement.Id("@Id", entry.Id),
            RecordStatement.Id("@RequirementId", entry.RequirementId),
            RecordStatement.Value("@SentAt", entry.SentAt),
            RecordStatement.Value("@Recipient", entry.Recipient)),
    ];

    /// <summary>Starts the organization's records at revision 1; writes nothing when they were already started.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <returns>The statements; the last answers 1 when this started them.</returns>
    /// <remarks>The revision has no public id of its own (one per organization), so its row's Id is the organization's id.</remarks>
    public static IReadOnlyList<RecordStatement> StartRevision(Guid organizationId) =>
    [
        RecordStatement.Of(
            "StartOrganizationRevision",
            """
            INSERT INTO doc.OrganizationRevision (Id, OrganizationId, Revision)
            SELECT @OrganizationId, @OrganizationId, 1
             WHERE NOT EXISTS (SELECT 1 FROM doc.OrganizationRevision WITH (UPDLOCK, HOLDLOCK) WHERE OrganizationId = @OrganizationId);
            """,
            RecordStatement.Id("@OrganizationId", organizationId)),
    ];

    /// <summary>Adds a link from a document, once FDW's link rule passed it.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="link">The link.</param>
    /// <returns>The statements.</returns>
    public static IReadOnlyList<RecordStatement> AddLink(Guid organizationId, IDocumentLink link) =>
    [
        DocumentFound(organizationId, link.DocumentId),
        RecordStatement.Of(
            "AddDocumentLink",
            """
            INSERT INTO doc.DocumentLink (Id, OrganizationId, DocumentId, LinkKind, TargetKind, TargetId)
            VALUES (@Id, @OrganizationId, @DocumentId, @LinkKind, @TargetKind, @TargetId);
            """,
            RecordStatement.Id("@OrganizationId", organizationId),
            RecordStatement.Id("@Id", link.Id),
            RecordStatement.Id("@DocumentId", link.DocumentId),
            RecordStatement.Value("@LinkKind", link.LinkKind),
            RecordStatement.Value("@TargetKind", link.TargetKind),
            RecordStatement.Id("@TargetId", link.TargetId)),
    ];

    /// <summary>Removes a link: its row stays, with the time it was removed, so what a document was linked to stays on record.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="id">The link.</param>
    /// <returns>The statements.</returns>
    public static IReadOnlyList<RecordStatement> RemoveLink(Guid organizationId, Guid id) =>
    [
        RecordStatement.Finding(
            "FindLink",
            "Link",
            id,
            "SELECT CASE WHEN EXISTS (SELECT 1 FROM doc.DocumentLink WHERE OrganizationId = @OrganizationId AND Id = @Id AND RemovedAt IS NULL) THEN 1 ELSE 0 END;",
            RecordStatement.Id("@OrganizationId", organizationId),
            RecordStatement.Id("@Id", id)),
        RecordStatement.Of(
            "RemoveDocumentLink",
            """
            UPDATE doc.DocumentLink
               SET RemovedAt = sysdatetimeoffset(), ModifyDate = sysdatetimeoffset(), ModifyBy = suser_sname()
             WHERE OrganizationId = @OrganizationId AND Id = @Id AND RemovedAt IS NULL;
            """,
            RecordStatement.Id("@OrganizationId", organizationId),
            RecordStatement.Id("@Id", id)),
    ];

    private static RecordStatement DocumentFound(Guid organizationId, Guid documentId) => RecordStatement.Finding(
        "FindDocument",
        "Document",
        documentId,
        "SELECT CASE WHEN EXISTS (SELECT 1 FROM doc.DocumentRecord WHERE OrganizationId = @OrganizationId AND Id = @DocumentId) THEN 1 ELSE 0 END;",
        RecordStatement.Id("@OrganizationId", organizationId),
        RecordStatement.Id("@DocumentId", documentId));

    private static RecordStatement NodeFound(Guid organizationId, Guid recordId) => RecordStatement.Finding(
        "FindRecord",
        "Record",
        recordId,
        "SELECT CASE WHEN EXISTS (SELECT 1 FROM doc.RecordNode WHERE OrganizationId = @OrganizationId AND Id = @RecordId) THEN 1 ELSE 0 END;",
        RecordStatement.Id("@OrganizationId", organizationId),
        RecordStatement.Id("@RecordId", recordId));
}
