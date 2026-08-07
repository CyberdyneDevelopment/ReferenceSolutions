# FractalDataWorks Reference ETL Server

A reference implementation of an ETL server built with the [FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks) framework.

## Overview

This project demonstrates how to build a production-ready ETL pipeline server using FractalDataWorks patterns:

- **Pipeline execution** with tracking and status reporting
- **TypeCollections** for extensible enums and plugin architecture
- **Railway-oriented programming** with `IGenericResult<T>`
- **MessageLogging** for structured logging
- **Three-phase DI** (Configure/Register/Initialize) for service registration

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) (preview)
- SQL Server (for data storage)

## Getting Started

```bash
# Build
dotnet build -c Release

# Run tests
dotnet test

# Run the ETL server
dotnet run --project src/Reference.Etl.Server
```

## Related Projects

- [FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks) - Core framework
- [Reference API](https://github.com/CyberdyneDevelopment/reference-api) - API server reference
- [Reference Scheduler](https://github.com/CyberdyneDevelopment/reference-scheduler) - Scheduler server reference
- [Reference UI](https://github.com/CyberdyneDevelopment/reference-ui) - Management UI reference

## License

Licensed under the Apache License 2.0. See [LICENSE](LICENSE) for details.
