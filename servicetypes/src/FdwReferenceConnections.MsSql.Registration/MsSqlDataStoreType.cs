using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.MsSql;

/// <summary>
/// DataStore type for Microsoft SQL Server.
/// Provides container building using TableContainer, ViewContainer, and DatabasePath.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataStoreTypes), "MsSql")]
public sealed class MsSqlDataStoreType : DataStoreTypeBase<IDataStoreImplementationConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlDataStoreType"/> class.
    /// </summary>
    public MsSqlDataStoreType() : base(
        name: "MsSql",
        displayName: "SQL Server DataStore",
        description: "Microsoft SQL Server data store")
    {

    }

    /// <inheritdoc />
    /// <remarks>
    /// Nothing to register. MsSql has no DataStore typed body.
    /// </remarks>


    /// <inheritdoc />
    // Why: the MsSql transport supplies its FK-aware, typed-field builder. Replaces the never-called
    // per-container Build that returned TableContainer/ViewContainer (both deleted by the redesign).
    public override IDataStoreBuilder SupplyBuilder(ILogger? logger = null)
        => new MsSqlDataStoreBuilder(logger);

}
