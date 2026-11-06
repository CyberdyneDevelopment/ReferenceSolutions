using System;
using Fdw.Documents.Records.Abstractions;
using Fdw.Results;
using Microsoft.Data.SqlClient;

namespace FdwReferenceConnections.DocumentRecords.Rows;

/// <summary>A row of doc.OrganizationRevision (FDW-3731).</summary>
internal sealed class OrganizationRevisionRow : IOrganizationRevision
{
    /// <summary>Initializes a new instance of the <see cref="OrganizationRevisionRow"/> class.</summary>
    /// <param name="organizationId">The OrganizationId.</param>
    /// <param name="revision">The Revision.</param>
    internal OrganizationRevisionRow(Guid organizationId, long revision)
    {
        OrganizationId = organizationId;
        Revision = revision;
    }

    /// <inheritdoc/>
    public Guid OrganizationId { get; }

    /// <inheritdoc/>
    public long Revision { get; }

    /// <summary>Reads the row the reader is on.</summary>
    /// <param name="reader">The reader, on a row of the statement that selects the columns by name.</param>
    /// <returns>The row.</returns>
    internal static IGenericResult<IOrganizationRevision> From(SqlDataReader reader) =>
        GenericResult<IOrganizationRevision>.Success(new OrganizationRevisionRow(
            reader.GetGuid(reader.GetOrdinal("OrganizationId")),
            reader.GetInt64(reader.GetOrdinal("Revision"))));
}
