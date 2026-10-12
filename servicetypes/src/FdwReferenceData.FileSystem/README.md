# Fdw.Data.FileSystem

File-system data support: reading and writing containers backed by files.

This package declares 1 interface(s), 1 service/provider type(s).

## Contracts (1)

| Type | Kind | Purpose |
|---|---|---|
| `IFileSystemCommand` | interface | Marker interface for the native FileSystem command type used by ConnectionBase&lt;TCommand, ...&gt;. |

## Services (1)

| Type | Kind | Purpose |
|---|---|---|
| `FileSystemCommandTranslator` | class | Translates a universal + configured container into the native the FileSystem connection executes. Read… |

## Types (10)

| Type | Kind | Purpose |
|---|---|---|
| `ContainerRecordOptions` | class | Builds the format-specific / for a configured container DYNAMICALLY from its and field schema — the… |
| `FileSystemConfigurationDeleteCommand` | class | The native FileSystem command that soft-deletes a configuration record (the version-on-write… |
| `FileSystemConfigurationSaveCommand` | class | The native FileSystem command that persists a NEW logical version of a configuration record (the… |
| `FileSystemConfigurationWriteLog` | class | MessageLogging for the FileSystem configuration write path — the version-on-write CREATE… |
| `FileSystemContainerPath` | class | for a FileSystem container's physical address: the FULL relative file path (the owning DataPath's folder… |
| `FileSystemReadCommand` | class | The native FileSystem command that reads records from a configured file container through the… |
| `FileSystemRecordCommand` | class | Base for the native FileSystem commands produced by from an IDataCommand + container. A native command… |
| `FileSystemUpdateCommand` | class | The native FileSystem command that mutates existing rows IN PLACE (the literal, non-versioning Update… |
| `FileSystemWriteCommand` | class | The native FileSystem command that writes records to a configured file container through the… |
| `PathCanonicalizer` | class | Resolves relative paths against a connection Root and enforces sandbox isolation. |

## Installation

```bash
dotnet add package Fdw.Data.FileSystem --prerelease
```

## Dependencies

`Fdw.Commands.Data.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.Data.RowSources.Abstractions` · `Fdw.Data.RowSources.FixedWidth.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Results` · `Fdw.Services.Connections.FileSystem.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
