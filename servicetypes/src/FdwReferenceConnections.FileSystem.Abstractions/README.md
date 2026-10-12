# Fdw.Services.Connections.FileSystem.Abstractions

Contracts for the file-system connection.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (2)

| Type | Kind | Purpose |
|---|---|---|
| `IFileSystemClient` | interface | Typed primitive client for file system I/O. All paths are relative to the connection's Root; the… |
| `IFileSystemConnection` | interface | Marker interface for a FileSystem connection. Exposes the root directory path and the typed primitive… |

## Base types (2)

| Type | Kind | Purpose |
|---|---|---|
| `FileSystemResultCodeBase` | class | Base class for FileSystem connection result codes. |
| `FileSystemResultCodes` | class | TypeCollection for FileSystem connection result codes. Result codes use categorized catalog numbers… |

## Models and supporting types (9)

| Type | Kind | Purpose |
|---|---|---|
| `FileNotFoundCode` | class | The file at the connector path was not found. |
| `FileSystemConnectionLog` | class | MessageLogging for the FileSystem connection. EventId range: 9550-9574 |
| `FileSystemRecordConnectorLog` | class | MessageLogging for the FileSystem config-driven record read/write seam (the record source/writer path… |
| `FileWriteCapability` | class | File write capability — writes content to a file accessible through the connection. Used by file-system… |
| `IoFailedCode` | class | A general I/O failure occurred during a file operation. |
| `PathOutsideRootCode` | class | The resolved path is outside the connection Root directory. |
| `PathTraversalDeniedCode` | class | The requested path resolves outside the connection Root (path traversal attempt). |
| `RootDirectoryDoesNotExistCode` | class | The FileSystemConnection Root directory does not exist on disk. |
| `RootNotConfiguredCode` | class | The FileSystemConnection is missing a required Root directory. |

## Installation

```bash
dotnet add package Fdw.Services.Connections.FileSystem.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Configuration.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Results.Abstractions` · `Fdw.Services.Connections.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
