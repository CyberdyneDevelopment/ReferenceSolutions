using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Abstractions.DocumentRecords;
using FdwReferenceConnections.MsSql;

namespace FdwReferenceConnections.DocumentRecords;

/// <summary>
/// The DocumentRecord extension of the SQL Server connection (FDW-3731): wraps a <see cref="MsSqlConnection"/> in a
/// <see cref="MsSqlDocumentRecordConnection"/>. It allocates only: FDW asks it on every grant check and every record store lookup.
/// </summary>
[TypeOption(typeof(DocumentRecordConnectionTypes), "MsSql")]
public sealed class MsSqlDocumentRecordConnectionType : DocumentRecordConnectionTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="MsSqlDocumentRecordConnectionType"/> class.</summary>
    public MsSqlDocumentRecordConnectionType() : base("MsSql")
    {
    }

    /// <inheritdoc/>
    public override IDocumentRecordConnection? Adapt(IGenericConnection connection) =>
        connection is MsSqlConnection sqlServer ? new MsSqlDocumentRecordConnection(sqlServer) : null;
}
