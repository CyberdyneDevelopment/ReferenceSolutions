using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Data.Formats;
using Fdw.Data.MsSql;
using Fdw.Results;
using Fdw.Schema;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.MsSql.Logging;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Fdw.Services.Connections.MsSql;

using Fdw.Services.Connections.MsSql.Discovery;

using Fdw.Services.Connections.MsSql.Authentication;

using Fdw.Services.Connections.MsSql.Limits;

using Fdw.Services.Connections.MsSql.ErrorHandlers;

using ReferenceConnections.MsSql.Mapping;

using Fdw.Services.Connections.MsSql.Messages;

using Fdw.Services.Connections.MsSql.Results;

using Fdw.Services.Connections.MsSql.Commands;

using Fdw.Services.Connections.MsSql.Validation;

namespace ReferenceConnections.MsSql;

/// <summary>
/// DataStore type for Microsoft SQL Server.
/// Provides container building using TableContainer, ViewContainer, and DatabasePath.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataStoreTypes), "MsSql")]
public sealed class MsSqlDataStoreType : DataStoreTypeBase<DataStoreConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlDataStoreType"/> class.
    /// </summary>
    public MsSqlDataStoreType() : base(
        name: "MsSql",
        sectionName: "MsSql",
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
