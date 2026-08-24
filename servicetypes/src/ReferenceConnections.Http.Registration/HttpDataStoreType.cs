using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Builders;
using Fdw.Services.Connections.Http.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Fdw.Services.Connections;
using Fdw.Services.Connections.Http;

using Fdw.Services.Connections.Http.Limits;

using Fdw.Services.Connections.Http.Results;

using Fdw.Services.Connections.Http.Commands;

using Fdw.Services.Connections.Http.Validation;

using Fdw.Services.Connections.Http.Protocols;

using Fdw.Services.Connections.Http.Security;

namespace ReferenceConnections.Http;

/// <summary>
/// DataStore type for HTTP transports (REST/GeoJSON/etc.).
/// HTTP is a non-SQL transport, so it supplies the generic builder, which builds generic
/// DataContainer nodes whose response Format/metadata come from the resolved container config
/// (RecordSelector + format), with a GenericContainerPath physical address carrying the request path.
/// </summary>
/// <remarks>
/// Why: the redesign requires one per-transport <see cref="IDataStoreBuilder"/> resolved via
/// <see cref="DataStoreTypes"/>.<c>ByName(store.ServiceOptionType)</c>. The earlier cleanup deleted the
/// protocol/format-named Rest/Soap/File store types (correctly — those were bogus), but the transport
/// "Http" still needs a DataStoreType so an Http-backed store builds; without it every Http DataStore
/// is dropped at tree build (EventId 5099) and HTTP source reads fail. Body-less: an Http DataStore has
/// no typed body table, so no typed config provider is registered — the base
/// <see cref="DataStoreConfigurationProvider"/> composes the header + cascaded Paths/Containers/Fields.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataStoreTypes), "Http")]
public sealed class HttpDataStoreType : DataStoreTypeBase<DataStoreConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpDataStoreType"/> class.
    /// </summary>
    public HttpDataStoreType() : base(
        name: "Http",
        sectionName: "Http",
        displayName: "HTTP DataStore",
        description: "HTTP/REST data store (transport inherited from an HTTP connection)")
    {

    }

    /// <inheritdoc />
    /// <remarks>
    /// No required services — DataStore instances are assembled by the per-transport
    /// IDataStoreBuilder (SupplyBuilder); the legacy IDataStoreFactory build path was removed.
    /// </remarks>


    /// <inheritdoc />
    // Why: HTTP is a non-SQL transport — it supplies the generic builder (same as FileSystem). The
    // builder builds generic DataContainer nodes whose Format/metadata come from the container config's
    // resolved format and whose physical address is a GenericContainerPath carrying the request path.
    // Why: the transport boundary owns the ConnectionTypes lookup so Fdw.Data.DataNodes (where
    // GenericDataStoreBuilder lives) stays connection-agnostic. ByName on an unknown name yields the
    // NotFound connection type option, whose DefaultResponseFormat is FormatTypes.NotFound — fail-loud
    // preserved, never a silent substitute.
    public override IDataStoreBuilder SupplyBuilder(ILogger? logger = null)
        => new GenericDataStoreBuilder(ConnectionTypes.ByName("Http").DefaultResponseFormat, logger);

}
