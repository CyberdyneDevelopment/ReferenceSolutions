using System;
using Fdw.Documents.Records.Abstractions.Definitions;
using Fdw.Results;
using Microsoft.Data.SqlClient;

namespace FdwReferenceConnections.DocumentRecords.Rows;

/// <summary>A row of doc.AttributeDefinition (FDW-3731).</summary>
internal sealed class AttributeDefinitionRow : IAttributeDefinition
{
    /// <summary>Initializes a new instance of the <see cref="AttributeDefinitionRow"/> class.</summary>
    /// <param name="id">The Id.</param>
    /// <param name="code">The Code.</param>
    /// <param name="tenantId">The TenantId.</param>
    /// <param name="organizationId">The OrganizationId.</param>
    /// <param name="locked">The Locked.</param>
    /// <param name="name">The Name.</param>
    /// <param name="dataType">The DataType.</param>
    /// <param name="appliesTo">The AppliesTo.</param>
    internal AttributeDefinitionRow(Guid id, string code, Guid? tenantId, Guid? organizationId, bool locked, string name, string dataType, string appliesTo)
    {
        Id = id;
        Code = code;
        TenantId = tenantId;
        OrganizationId = organizationId;
        Locked = locked;
        Name = name;
        DataType = dataType;
        AppliesTo = appliesTo;
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
    public string Name { get; }

    /// <inheritdoc/>
    public string DataType { get; }

    /// <inheritdoc/>
    public string AppliesTo { get; }

    /// <summary>Reads the row the reader is on.</summary>
    /// <param name="reader">The reader, on a row of the statement that selects the columns by name.</param>
    /// <returns>The row.</returns>
    internal static IGenericResult<IAttributeDefinition> From(SqlDataReader reader) =>
        GenericResult<IAttributeDefinition>.Success(new AttributeDefinitionRow(
            reader.GetGuid(reader.GetOrdinal("Id")),
            reader.GetString(reader.GetOrdinal("Code")),
            reader.IsDBNull(reader.GetOrdinal("TenantId")) ? null : reader.GetGuid(reader.GetOrdinal("TenantId")),
            reader.IsDBNull(reader.GetOrdinal("OrganizationId")) ? null : reader.GetGuid(reader.GetOrdinal("OrganizationId")),
            reader.GetBoolean(reader.GetOrdinal("Locked")),
            reader.GetString(reader.GetOrdinal("Name")),
            reader.GetString(reader.GetOrdinal("DataType")),
            reader.GetString(reader.GetOrdinal("AppliesTo"))));
}
