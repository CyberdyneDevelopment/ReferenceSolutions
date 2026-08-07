# Reference.Scheduler.Server

ASP.NET Core scheduling server. Manages schedules (cron/interval/once/manual) in ConfigurationDb and triggers downstream jobs against the ETL server.

- **TargetFramework:** `net10.0`
- **SDK:** `Microsoft.NET.Sdk.Web`

## What's inside

| Path | Notes |
|------|-------|
| `Program.cs` | Standard FDW startup + three-phase ServiceTypeCollection registration + FastEndpoints/Scalar |
| `configurationSchema.json` | Startup connection to ConfigurationDb + EnvSecrets. Copied to output. |
| `Endpoints/CreateScheduleEndpoint.cs` | `POST` create schedule |
| `Endpoints/GetScheduleEndpoint.cs` | `GET` schedule by id |
| `Endpoints/ListSchedulesEndpoint.cs` | `GET` list |
| `Endpoints/UpdateScheduleEndpoint.cs` | `PUT` update |
| `Endpoints/DeleteScheduleEndpoint.cs` | `DELETE` remove |
| `Services/`, `Queries/`, `Models/`, `Configuration/`, `Validators/`, `Validation/`, `Logging/`, `sql/` | Supporting classes and inline SQL |
| `appsettings.json` | `Kestrel:Endpoints:Http:Url = http://+:5004`; `PipelineJobClient.BaseUrl = http://localhost:5002`; `ApiBaseUrl = https://localhost:5001` |

## Key dependencies (from `Reference.Scheduler.Server.csproj`)

- **Hosting:** `FractalDataWorks.Hosting.MsSql`
- **Web:** `FastEndpoints`, `FastEndpoints.Swagger`, `FluentValidation`, `Scalar.AspNetCore`
- **Scheduling:** `FractalDataWorks.Services.Scheduling`
- **Data:** `FractalDataWorks.Services.Data`, `FractalDataWorks.Data`, `FractalDataWorks.Commands.Data.Extensions`, `FractalDataWorks.Services.Connections[.MsSql]`
- **Clients:** `FractalDataWorks.Services.Pipelines.Clients` (calls ETL server)
- **Multitenancy:** `FractalDataWorks.Services.Multitenancy.Abstractions`
- **Configuration / Secrets:** `FractalDataWorks.Configuration.MsSql`, `FractalDataWorks.Services.SecretManagers[.EnvironmentVariable]`
- **Source generators:** `Collections`, `Configuration`, `Data`, `MessageLogging`, `Registration` (all `.SourceGenerators`)

## Run

```bash
dotnet run --project Reference.Scheduler.Server.csproj
```

Listens on `http://localhost:5004`. Staging deployment on VM 104 listens on `:5024`.
