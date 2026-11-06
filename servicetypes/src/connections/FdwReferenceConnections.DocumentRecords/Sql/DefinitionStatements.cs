using System;

namespace FdwReferenceConnections.DocumentRecords.Sql;

/// <summary>
/// The reads of the document definitions that bear on an organization (FDW-3731, FDW-3398): the platform's, its tenant's and, when an
/// organization is named, the organization's. FDW's <c>ScopedDefinitions</c> decides which of them is in force; this answers the rows.
/// </summary>
internal static class DefinitionStatements
{
    /// <summary>Reads document type definitions.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="organizationId">The organization, or null for the platform's and the tenant's only.</param>
    /// <returns>The statement.</returns>
    public static RecordStatement DocumentTypes(Guid tenantId, Guid? organizationId) =>
        Rows("ReadDocumentTypeDefinitions", "doc.DocumentTypeDefinition", ", Name", tenantId, organizationId);

    /// <summary>Reads attribute definitions.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="organizationId">The organization, or null for the platform's and the tenant's only.</param>
    /// <returns>The statement.</returns>
    public static RecordStatement Attributes(Guid tenantId, Guid? organizationId) =>
        Rows("ReadAttributeDefinitions", "doc.AttributeDefinition", ", Name, DataType, AppliesTo", tenantId, organizationId);

    /// <summary>Reads record kind definitions.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="organizationId">The organization, or null for the platform's and the tenant's only.</param>
    /// <returns>The statement.</returns>
    public static RecordStatement RecordKinds(Guid tenantId, Guid? organizationId) =>
        Rows("ReadRecordKindDefinitions", "doc.RecordKindDefinition", ", Name", tenantId, organizationId);

    /// <summary>Reads workflow stage definitions.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="organizationId">The organization, or null for the platform's and the tenant's only.</param>
    /// <returns>The statement.</returns>
    public static RecordStatement WorkflowStages(Guid tenantId, Guid? organizationId) =>
        Rows("ReadWorkflowStageDefinitions", "doc.WorkflowStageDefinition", ", Name, DocumentType, StageOrder", tenantId, organizationId);

    /// <summary>Reads requirement rule definitions.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="organizationId">The organization, or null for the platform's and the tenant's only.</param>
    /// <returns>The statement.</returns>
    public static RecordStatement RequirementRules(Guid tenantId, Guid? organizationId) =>
        Rows("ReadRequirementRuleDefinitions", "doc.RequirementRuleDefinition", ", RecordKind, ExpectedDocumentType", tenantId, organizationId);

    /// <summary>Reads link kind definitions.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="organizationId">The organization, or null for the platform's and the tenant's only.</param>
    /// <returns>The statement.</returns>
    public static RecordStatement LinkKinds(Guid tenantId, Guid? organizationId) =>
        Rows("ReadLinkKindDefinitions", "doc.LinkKindDefinition", ", Name", tenantId, organizationId);

    // The table and columns are this class's constants, never a caller's text.
    private static RecordStatement Rows(string name, string table, string columns, Guid tenantId, Guid? organizationId) => RecordStatement.Of(
        name,
        $"""
        SELECT Id, Code, TenantId, OrganizationId, Locked{columns}
          FROM {table}
         WHERE (TenantId IS NULL AND OrganizationId IS NULL)
            OR (TenantId = @TenantId AND OrganizationId IS NULL)
            OR (@OrganizationId IS NOT NULL AND TenantId = @TenantId AND OrganizationId = @OrganizationId)
         ORDER BY Code, RowId;
        """,
        RecordStatement.Id("@TenantId", tenantId),
        RecordStatement.Id("@OrganizationId", organizationId));
}
