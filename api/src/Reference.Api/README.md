# Reference.Api

ASP.NET Core HTTP API server demonstrating the canonical FractalDataWorks consumer-app shape: startup from `configurationSchema.json`, three-phase DI registration, FastEndpoints routing, Scalar API browser, FDW domain `.Endpoints` packages, Serilog → Seq/Loki, OpenTelemetry.

- **TargetFramework:** `net10.0`
- **SDK:** `Microsoft.NET.Sdk.Web`
- **RootNamespace:** `Reference.Api`

## What's inside

| Path | Notes |
|------|-------|
| `Program.cs` | Serilog → builder → forwarded headers → AddConfigurationGateway(configurationSchema.json) → three-phase ServiceTypeCollection registration → FastEndpoints/Scalar → Run |
| `configurationSchema.json` | Startup connection to ConfigurationDb + EnvSecrets secret manager. Copied to output. |
| `Endpoints/` | One file per endpoint, grouped per domain folder: `DataSets/`, `Connections/`, `Pipelines/`, `Users/`, `Tenants/`, `Roles/`, `SessionState/`, `Auth/`, `Calculations/`, `Catalog/`, `Quality/`, `Configuration/`, `FieldMappings/`, `Lineage/`, `Notifications/`, `Audit/`, `Visualization/`, `Analytics/`, `Health/`, `Proxy/`, `Bulk/`, `Nfl/`, `Promotion/`, `Shared/` |
| `Endpoints/PublicDataEndpoint.cs`, `ProtectedEndpoint.cs`, `AuthenticatedDataEndpoint.cs`, `AdminDataEndpoint.cs`, `SearchEndpoint.cs` | Top-level endpoints |
| `Validators/`, `Middleware/`, `Constants/`, `Logging/`, `Templates/` | Supporting classes |
| `appsettings.json`, `appsettings.Development.json`, `appsettings.Production.json` | Serilog + ApiClients + TenantProviders |

## Key dependencies (from `Reference.Api.csproj`)

- **Hosting:** `FractalDataWorks.Hosting.MsSql`
- **Web:** `FastEndpoints`, `FastEndpoints.Swagger`, `Scalar.AspNetCore`, `FractalDataWorks.Web.Api`
- **Domain endpoints:** `FractalDataWorks.Services.{Connections,Data,Pipelines,Scheduling,Users,Authorization,Multitenancy,Quality,Catalog,Notifications,Settings,Messaging,Authentication,SecretManagers}.Endpoints`, `FractalDataWorks.Schema.Endpoints`, `FractalDataWorks.Operations.Endpoints`, `FractalDataWorks.Calculations.Endpoints`, `FractalDataWorks.Web.Search.Endpoints`
- **Configuration:** `FractalDataWorks.Configuration.MsSql`, `FractalDataWorks.Configuration.Endpoints`, `FractalDataWorks.Configuration.Writers`
- **Connections:** `FractalDataWorks.Services.Connections.{MsSql,PostgreSql,Http,FileSystem,RoslynWorkspace}`
- **Secrets:** `FractalDataWorks.Services.SecretManagers.{EnvironmentVariable,MsSql}`
- **Auth:** `FractalDataWorks.Services.Authentication.Jwt[.MsSql]`, `Microsoft.AspNetCore.Authentication.JwtBearer`
- **Source generators:** `FractalDataWorks.MessageLogging.SourceGenerators`, `Data.SourceGenerators`, `Registration.SourceGenerators`, `Collections.SourceGenerators`, `Configuration.SourceGenerators`
- **Project ref:** `..\Reference.Nfl\Reference.Nfl.csproj`

## Run

```bash
dotnet run --project Reference.Api.csproj
```

Defaults to Kestrel `http://localhost:5000` (no URL pinned in appsettings). Staging deployment on VM 104 binds to `:5020`.
