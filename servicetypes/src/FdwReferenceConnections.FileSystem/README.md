# Fdw.Services.Connections.FileSystem

The file-system connection. Not split into a separate aggregation package, because FDW's own configuration source is itself a file-system connection.

Registration lives in the option, not in a host's `Program.cs`. Each option carries three replaceable phase bodies — **Configure**, **Register**, **Initialize** — where the first two take and return the `IHostApplicationBuilder` and the third takes and returns the `IServiceProvider`. Referencing this package is what enlists its options: a module initializer collects and dedupes them at assembly load.

## Options (1 declared)

| Type | Kind | Purpose |
|---|---|---|
| `FileSystemConnectionType` | class | Connection type definition for FileSystem connections. Registers and the typed-body provider. Connection… |

Shipped options are reference implementations, not canon — a consumer adds a kind by declaring its own option against this collection, in its own assembly.

## Configuration

Configuration classes are `[ManagedConfiguration]`: they generate their own DDL, validation and UI form metadata, and are read back as rows rather than from JSON.

| Type | Kind | Purpose |
|---|---|---|
| `FileSystemConnectionConfiguration` | class | Configuration for FileSystem connections. Standalone typed body POCO — no longer inherits from .… |

## Installation

```bash
dotnet add package Fdw.Services.Connections.FileSystem --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Configuration.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.Data.DataNodes` · `Fdw.Data.DataSets.Abstractions` · `Fdw.Data.FileSystem` · `Fdw.Data.RowSources` · `Fdw.Data.RowSources.Abstractions` · `Fdw.Data.RowSources.FixedWidth.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Results` · `Fdw.Results.Abstractions` · `Fdw.Services` · `Fdw.Services.Abstractions` · `Fdw.Services.Authorization.Abstractions` · `Fdw.Services.Connections` · `Fdw.Services.Connections.Abstractions` · `Fdw.Services.Connections.FileSystem.Abstractions` · `Fdw.Services.Data` · `Fdw.Services.Data.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
