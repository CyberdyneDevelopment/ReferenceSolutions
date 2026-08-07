# FractalDataWorks Reference API

A reference implementation of an API server built with the [FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks) framework.

## Overview

This project demonstrates how to build a production-ready API using FractalDataWorks patterns:

- **FastEndpoints** for minimal API routing
- **TypeCollections** for extensible enums and plugin architecture
- **Railway-oriented programming** with `IGenericResult<T>`
- **MessageLogging** for structured logging
- **Three-phase DI** (Configure/Register/Initialize) for service registration
- **ManagedConfiguration** for database-backed configuration

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) (preview)
- SQL Server (for data storage)

## Getting Started

```bash
# Build
dotnet build -c Release

# Run tests
dotnet test

# Run the API server
dotnet run --project src/Reference.Api
```

## Project Structure

```
src/
  Reference.Api/          # API server with FastEndpoints
tests/
  Reference.Api.Tests/    # Unit and integration tests
docker/
  mssql/                  # SQL Server Docker setup with seed data
```

## JWT Secret Key Configuration

The API uses JWT for authentication. The signing key can be configured two ways:

### Development (Direct Key)

In development, the signing key is loaded directly from `cfg.JwtAuthentication.SecretKey`. The seed data provides a hardcoded dev key. No secret manager is needed.

### Production (Secret Manager Resolution)

When `SecretManagerName` is populated in `cfg.JwtAuthentication`, the `JwtAuthenticationFactory` resolves the signing key from the named secret manager at service creation time. The direct `SecretKey` column is ignored.

**Resolution order:** SecretManagers initialize before Authentication in the three-phase startup, so the secret manager is always available when the JWT factory runs.

#### Azure Key Vault with System-Assigned Managed Identity

In Azure, the ConfigDb connection uses Entra authentication (no password), so AKV can be configured entirely through the configuration database — no environment variables needed.

**Step 1:** Create the AKV secret manager in `cfg.SecretManager` + `cfg.AzureKeyVaultSecretManager`:

```sql
-- Parent: cfg.SecretManager
DECLARE @AkvId UNIQUEIDENTIFIER = NEWID();

INSERT INTO cfg.SecretManager (Id, Name, ServiceOptionType, Description)
VALUES (
    @AkvId,
    'ProductionKeyVault',
    'AzureKeyVault',
    'Azure Key Vault with system-assigned managed identity'
);

-- Child: cfg.AzureKeyVaultSecretManager
INSERT INTO cfg.AzureKeyVaultSecretManager (
    SecretManagerId, VaultUri, AuthenticationMethod, ValidateOnStartup
)
VALUES (
    @AkvId,
    'https://your-vault.vault.azure.net/',
    'ManagedIdentity',    -- system-assigned (ManagedIdentityId left NULL)
    1                     -- validate connectivity at startup
);
```

**Step 2:** Point the JWT authentication at the AKV secret manager:

```sql
UPDATE jwt
SET jwt.SecretManagerName = 'ProductionKeyVault',
    jwt.SecretKeyName = 'jwt-signing-key'   -- name of the secret in AKV
FROM cfg.JwtAuthentication jwt
INNER JOIN cfg.Authentication a ON jwt.AuthenticationId = a.Id
WHERE a.Name = 'ApiJwtAuth' AND a.IsCurrent = 1 AND jwt.IsCurrent = 1;
```

The `SecretKey` column can remain populated (used as dev fallback) or be cleared — the factory always prefers the secret manager when `SecretManagerName` is set.

#### On-Premises with Environment Variables

For on-prem deployments, use the `EnvSecrets` secret manager (already seeded):

```sql
UPDATE jwt
SET jwt.SecretManagerName = 'EnvSecrets',
    jwt.SecretKeyName = 'JWT_SECRET_KEY'
FROM cfg.JwtAuthentication jwt
INNER JOIN cfg.Authentication a ON jwt.AuthenticationId = a.Id
WHERE a.Name = 'ApiJwtAuth' AND a.IsCurrent = 1 AND jwt.IsCurrent = 1;
```

Then set the environment variable on the host:

```bash
export FDW_SECRET_JWT_SECRET_KEY='your-production-signing-key-at-least-32-chars'
```

#### Authentication Methods

The AKV secret manager supports four credential types via `AuthenticationMethod`:

| Value | Use Case | Required Columns |
|-------|----------|------------------|
| `ManagedIdentity` | Azure VMs, App Service, AKS | `ManagedIdentityId` (NULL = system-assigned) |
| `ServicePrincipal` | CI/CD, external services | `TenantId`, `ClientId`, `ClientSecret` |
| `Certificate` | High-security environments | `TenantId`, `ClientId`, `CertificatePath`, `CertificatePassword` |
| `DeviceCode` | Interactive/developer scenarios | `TenantId`, `ClientId` |

For full documentation, see the [Secret Management wiki page](https://github.com/CyberdyneDevelopment/FractalDataWorks/wiki/12-10-Secret-Management).

## Related Projects

- [FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks) - Core framework
- [Reference ETL](https://github.com/CyberdyneDevelopment/reference-etl) - ETL server reference
- [Reference Scheduler](https://github.com/CyberdyneDevelopment/reference-scheduler) - Scheduler server reference
- [Reference UI](https://github.com/CyberdyneDevelopment/reference-ui) - Management UI reference

## License

Licensed under the Apache License 2.0. See [LICENSE](LICENSE) for details.
