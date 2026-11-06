using System;
using Fdw.Documents.Records.Abstractions.Definitions;
using Fdw.Results;
using Microsoft.Data.SqlClient;

namespace FdwReferenceConnections.DocumentRecords.Rows;

/// <summary>A row of doc.LinkKindDefinition (FDW-3731).</summary>
internal sealed class LinkKindDefinitionRow : ILinkKindDefinition
{
    /// <summary>Initializes a new instance of the <see cref="LinkKindDefinitionRow"/> class.</summary>
    /// <param name="id">The Id.</param>
    /// <param name="code">The Code.</param>
    /// <param name="tenantId">The TenantId.</param>
    /// <param name="organizationId">The OrganizationId.</param>
    /// <param name="locked">The Locked.</param>
    /// <param name="name">The Name.</param>
    internal LinkKindDefinitionRow(Guid id, string code, Guid? tenantId, Guid? organizationId, bool locked, string name)
    {
        Id = id;
        Code = code;
        TenantId = tenantId;
        OrganizationId = organizationId;
        Locked = locked;
        Name = name;
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

    /// <summary>Reads the row the reader is on.</summary>
    /// <param name="reader">The reader, on a row of the statement that selects the columns by name.</param>
    /// <returns>The row.</returns>
    internal static IGenericResult<ILinkKindDefinition> From(SqlDataReader reader) =>
        GenericResult<ILinkKindDefinition>.Success(new LinkKindDefinitionRow(
            reader.GetGuid(reader.GetOrdinal("Id")),
            reader.GetString(reader.GetOrdinal("Code")),
            reader.IsDBNull(reader.GetOrdinal("TenantId")) ? null : reader.GetGuid(reader.GetOrdinal("TenantId")),
            reader.IsDBNull(reader.GetOrdinal("OrganizationId")) ? null : reader.GetGuid(reader.GetOrdinal("OrganizationId")),
            reader.GetBoolean(reader.GetOrdinal("Locked")),
            reader.GetString(reader.GetOrdinal("Name"))));
}
