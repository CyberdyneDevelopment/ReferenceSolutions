using System;
using Fdw.Documents.Records.Abstractions;
using Fdw.Results;
using Microsoft.Data.SqlClient;

namespace FdwReferenceConnections.DocumentRecords.Rows;

/// <summary>A row of doc.RecordNode (FDW-3731).</summary>
internal sealed class RecordNodeRow : IRecordNode
{
    /// <summary>Initializes a new instance of the <see cref="RecordNodeRow"/> class.</summary>
    /// <param name="id">The Id.</param>
    /// <param name="organizationId">The OrganizationId.</param>
    /// <param name="kind">The Kind.</param>
    /// <param name="parentId">The ParentId.</param>
    /// <param name="name">The Name.</param>
    internal RecordNodeRow(Guid id, Guid organizationId, string kind, Guid? parentId, string name)
    {
        Id = id;
        OrganizationId = organizationId;
        Kind = kind;
        ParentId = parentId;
        Name = name;
    }

    /// <inheritdoc/>
    public Guid Id { get; }

    /// <inheritdoc/>
    public Guid OrganizationId { get; }

    /// <inheritdoc/>
    public string Kind { get; }

    /// <inheritdoc/>
    public Guid? ParentId { get; }

    /// <inheritdoc/>
    public string Name { get; }

    /// <summary>Reads the row the reader is on.</summary>
    /// <param name="reader">The reader, on a row of the statement that selects the columns by name.</param>
    /// <returns>The row.</returns>
    internal static IGenericResult<IRecordNode> From(SqlDataReader reader) =>
        GenericResult<IRecordNode>.Success(new RecordNodeRow(
            reader.GetGuid(reader.GetOrdinal("Id")),
            reader.GetGuid(reader.GetOrdinal("OrganizationId")),
            reader.GetString(reader.GetOrdinal("Kind")),
            reader.IsDBNull(reader.GetOrdinal("ParentId")) ? null : reader.GetGuid(reader.GetOrdinal("ParentId")),
            reader.GetString(reader.GetOrdinal("Name"))));
}
