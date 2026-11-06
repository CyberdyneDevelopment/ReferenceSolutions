using System;
using Fdw.Documents.Records.Abstractions.Definitions;
using Fdw.Results;
using Microsoft.Data.SqlClient;

namespace FdwReferenceConnections.DocumentRecords.Rows;

/// <summary>A row of doc.RequirementRuleDefinition (FDW-3731).</summary>
internal sealed class RequirementRuleDefinitionRow : IRequirementRuleDefinition
{
    /// <summary>Initializes a new instance of the <see cref="RequirementRuleDefinitionRow"/> class.</summary>
    /// <param name="id">The Id.</param>
    /// <param name="code">The Code.</param>
    /// <param name="tenantId">The TenantId.</param>
    /// <param name="organizationId">The OrganizationId.</param>
    /// <param name="locked">The Locked.</param>
    /// <param name="recordKind">The RecordKind.</param>
    /// <param name="expectedDocumentType">The ExpectedDocumentType.</param>
    internal RequirementRuleDefinitionRow(Guid id, string code, Guid? tenantId, Guid? organizationId, bool locked, string recordKind, string expectedDocumentType)
    {
        Id = id;
        Code = code;
        TenantId = tenantId;
        OrganizationId = organizationId;
        Locked = locked;
        RecordKind = recordKind;
        ExpectedDocumentType = expectedDocumentType;
    }

    /// <inheritdoc/>
    public Guid Id { get; }

    /// <inheritdoc/>
    public string Code { get; }

    /// <inheritdoc/>
    public Guid? TenantId { get; }

    /// <inheritdoc/>
    public Guid? OrganizationId { get; }

    /// <inheritdoc/>
    public bool Locked { get; }

    /// <inheritdoc/>
    public string RecordKind { get; }

    /// <inheritdoc/>
    public string ExpectedDocumentType { get; }

    /// <summary>Reads the row the reader is on.</summary>
    /// <param name="reader">The reader, on a row of the statement that selects the columns by name.</param>
    /// <returns>The row.</returns>
    internal static IGenericResult<IRequirementRuleDefinition> From(SqlDataReader reader) =>
        GenericResult<IRequirementRuleDefinition>.Success(new RequirementRuleDefinitionRow(
            reader.GetGuid(reader.GetOrdinal("Id")),
            reader.GetString(reader.GetOrdinal("Code")),
            reader.IsDBNull(reader.GetOrdinal("TenantId")) ? null : reader.GetGuid(reader.GetOrdinal("TenantId")),
            reader.IsDBNull(reader.GetOrdinal("OrganizationId")) ? null : reader.GetGuid(reader.GetOrdinal("OrganizationId")),
            reader.GetBoolean(reader.GetOrdinal("Locked")),
            reader.GetString(reader.GetOrdinal("RecordKind")),
            reader.GetString(reader.GetOrdinal("ExpectedDocumentType"))));
}
