# Reference.Etl.Server

ASP.NET Core ETL pipeline execution server. Reads pipeline definitions from ConfigurationDb and runs them against connections registered there, with execution tracking and lineage capture.

- **TargetFramework:** `net10.0`
- **SDK:** `Microsoft.NET.Sdk.Web`

## What's inside

| Path | Notes |
|------|-------|
| `Program.cs` | Standard FDW startup + three-phase ServiceTypeCollection registration + FastEndpoints/Scalar |
| `configurationSchema.json` | Startup connection to ConfigurationDb + EnvSecrets. Copied to output. |
| `Endpoints/Trigger/UnifiedTriggerEndpoint.cs` | Unified pipeline trigger |
| `Endpoints/TriggerJobEndpoint.cs`, `GetJobStatusEndpoint.cs` | Legacy job trigger / status |
| `Endpoints/Executions/` | Execution lifecycle: `Approve`, `Cancel`, `Pause`, `Resume`, `ResumeTest`, `Step`, `GetExecutionStatus`, `InspectEdge`, `InspectTask` |
| `Endpoints/Lineage/` | `LineageGraphEndpoint`, `ExpandLineageNodeEndpoint` |
| `Endpoints/Nodes/` | Node CRUD: `Create`, `Get`, `Update`, `Delete` |
| `Services/JobTriggerSources/` | Trigger source implementations |
| `Models/`, `Validators/`, `Logging/` | Supporting classes |
| `appsettings.json` | `Kestrel:Endpoints:Http:Url = http://+:5002` |

## Key dependencies (from `Reference.Etl.Server.csproj`)

- **Hosting:** `FractalDataWorks.Hosting.MsSql`
- **Web:** `FastEndpoints`, `FastEndpoints.Swagger`, `Scalar.AspNetCore`
- **ETL/Pipelines:** `FractalDataWorks.Services.Etl[.Abstractions,.Projects]`, `FractalDataWorks.Services.Pipelines`, `FractalDataWorks.Operations[.Endpoints]`
- **Connectors:** `FractalDataWorks.Connectors[.LocalFile,.Http,.RoslynSymbol]`
- **Data/Connections:** `FractalDataWorks.Services.Data`, `FractalDataWorks.Services.Connections[.MsSql]`, `FractalDataWorks.Commands.Data.Extensions`
- **Configuration / Secrets:** `FractalDataWorks.Configuration.MsSql`, `FractalDataWorks.Services.SecretManagers[.EnvironmentVariable]`
- **Source generators:** `Collections`, `Configuration`, `MessageLogging`, `Registration` (all `.SourceGenerators`)

## Run

```bash
dotnet run --project Reference.Etl.Server.csproj
```

Listens on `http://localhost:5002`. Staging deployment on VM 104 listens on `:5022`.
