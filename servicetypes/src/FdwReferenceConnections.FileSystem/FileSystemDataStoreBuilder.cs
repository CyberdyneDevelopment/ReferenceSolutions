using FdwReferenceData.FileSystem;
using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data.Builders;
using Fdw.Services.Data.DataNodes;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.FileSystem;

/// <summary>
/// The FileSystem per-transport <see cref="DataStoreBuilderBase"/>. Builds generic
/// <see cref="DataContainer"/> nodes (like <c>GenericDataStoreBuilder</c>) whose format + metadata
/// come from the container config, but whose physical <see cref="IStorageContainer.Path"/> is the FULL
/// relative FILE path — <c>{DataPath folder}/{container name}{format.CanonicalFileExtension}</c> — so a
/// config header and its typed body under one DataPath resolve to DISTINCT files.
/// </summary>
/// <remarks>
/// Why a dedicated FileSystem builder (not the shared <c>GenericDataStoreBuilder</c>): the generic
/// builder addresses a container by the owning DataPath's name only (correct for HTTP, where the DataPath
/// IS the URL path). A file store instead needs a real file path — the container's OWN name plus a file
/// extension — exactly as <c>MsSqlDataStoreBuilder</c> composes a two-part <c>{schema}.{object}</c> address.
/// The extension is NOT derivable from the format name, so it is read from
/// <see cref="IFormatType.CanonicalFileExtension"/>; a format that declares none (empty, e.g. Tabular) is
/// not file-addressable and <see cref="ValidateConfiguration"/> fails loud (NO FALLBACKS) before any node
/// is built, rather than composing a bare, extension-less path.
/// </remarks>
/// <remarks>
/// Why this lives in <c>FdwReferenceConnections.FileSystem</c> (not <c>Fdw.Data.DataNodes</c>):
/// FileSystem-specific path composition and the <c>"File"</c> physical-path domain are transport
/// knowledge — it must not live above the connection layer, mirroring where
/// <c>Fdw.Services.Connections.MsSql</c>'s own <c>MsSqlDataStoreBuilder</c> lives.
/// <c>GenericDataStoreBuilder</c> stays in <c>Fdw.Data.DataNodes</c> because it is genuinely transport-agnostic.
/// </remarks>
public sealed class FileSystemDataStoreBuilder : DataStoreBuilderBase
{
    private readonly IFormatType _defaultResponseFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemDataStoreBuilder"/> class.
    /// </summary>
    /// <param name="defaultResponseFormat">
    /// The transport's declared default response format, used when a container config declares none. The
    /// FileSystem transport supplies <see cref="FormatTypes.NotFound"/> (each file container declares its
    /// own Format), so an unset Format resolves to NotFound and fails loud in <see cref="ValidateConfiguration"/>.
    /// </param>
    /// <param name="logger">Logger for build diagnostics.</param>
    public FileSystemDataStoreBuilder(IFormatType defaultResponseFormat, ILogger? logger = null)
        : base(logger)
    {
        _defaultResponseFormat = defaultResponseFormat;
    }

    /// <summary>Rejects any container whose resolved format is not file-addressable.</summary>
    /// <param name="config">The store configuration to validate.</param>
    /// <returns>Success if every container has a usable file extension; otherwise a failure.</returns>
    // Why: a file store rejects any container whose resolved format is not file-addressable BEFORE building
    // — the void-returning BuildContainer cannot surface a failure, so the fail-loud lives here. The NotFound
    // sentinel carries a non-empty "NotFound" extension, so it is caught by reference, not the IsNullOrEmpty test.
    protected override IGenericResult ValidateConfiguration(IDataStoreImplementationConfiguration config)
    {
        foreach (var pathCfg in config.Paths)
        {
            foreach (var containerCfg in pathCfg.Containers)
            {
                var format = ContainerComposition.ResolveFormat(containerCfg, _defaultResponseFormat);
                if (ReferenceEquals(format, FormatTypes.NotFound) || string.IsNullOrEmpty(format.CanonicalFileExtension))
                    return GenericResult.Failure(
                        DataStoreLoaderLog.FormatNotFileAddressable(Logger, containerCfg.Name, format.Name));
            }
        }

        return GenericResult.Success();
    }

    /// <summary>Builds a file-addressed <see cref="DataContainer"/> — the container's own name plus
    /// its resolved format's canonical file extension, appended to the owning DataPath's folder.</summary>
    /// <param name="containerCfg">The container configuration row.</param>
    /// <param name="parent">The container's parent path node.</param>
    /// <param name="fields">This container's already-built fields.</param>
    /// <param name="keys">This container's already-resolved keys.</param>
    /// <param name="referencingKeys">Other containers' keys that reference this one, or a failure.</param>
    /// <returns>The built container.</returns>
    protected override IDataContainer BuildContainer(
        DataContainerConfiguration containerCfg,
        IDataNodePath parent,
        IReadOnlyList<IDataField> fields,
        IReadOnlyList<IContainerKey> keys,
        IGenericResult<IReadOnlyList<ReferencingKeyBinding>> referencingKeys)
    {
        var containerType = string.IsNullOrEmpty(containerCfg.TypeId)
            ? ContainerTypes.ByName("Endpoint")
            : ContainerTypes.ByName(containerCfg.TypeId);

        var format = ContainerComposition.ResolveFormat(containerCfg, _defaultResponseFormat);

        DataStoreLoaderLog.ContainerSubtypeChosen(Logger, containerCfg.Name, containerType.Name);
        DataStoreLoaderLog.ContainerFieldsBuilt(Logger, containerCfg.Name, fields.Count);

        // Why: the FULL relative file path = {DataPath folder}/{container name}{canonical extension},
        // mirroring MsSqlDataStoreBuilder's {schema}.{object}. ValidateConfiguration has already rejected
        // any container whose format is not file-addressable, so CanonicalFileExtension is non-empty here.
        return new DataContainer(
            containerCfg.Name,
            containerCfg.Description,
            parent,
            fields,
            keys,
            referencingKeys,
            containerType,
            format,
            new FileSystemContainerPath($"{parent.Name}/{containerCfg.Name}{format.CanonicalFileExtension}"),
            ["Query"],
            ContainerComposition.BuildMetadata(containerCfg, format),
            Logger);
    }
}
