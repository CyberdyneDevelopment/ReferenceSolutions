# ManagementUI-Tailwind

Blazor Server management interface for FractalDataWorks with Tailwind CSS.

## Architecture

- **Hosting:** Blazor Server (Interactive Server)
- **UI Framework:** Tailwind CSS v4 with Cyberdyne theme
- **Authentication:** Cookie-based with JWT stored in server-side cookie (not SessionStorage)
- **API Communication:** Server-side HTTP calls via named `HttpClient` instances (no CORS needed)

## Setup

1. Configure API URL in `appsettings.json`:
   ```json
   {
     "ApiEndpoints": {
       "Api": "https://localhost:5001"
     }
   }
   ```

2. Build and run:
   ```bash
   dotnet build
   dotnet run
   ```

## Project Structure

```
├── Components/
│   ├── App.razor / Routes.razor / _Imports.razor
│   ├── Layout/                       # MainLayout, NavItem, EmptyLayout
│   └── Pages/                        # Razor pages
├── Logging/                          # Structured logging (MessageLogging)
├── Helpers/                          # Shared helpers
├── Styles/                           # Tailwind input CSS
├── wwwroot/                          # Compiled CSS
└── Program.cs                        # Startup, auth, API client registration
```

## API Clients

All domain API clients are registered via `ApiClientTypes.Configure` / `ApiClientTypes.Register`
(TypeCollection-driven discovery). Each client inherits from `ApiClientBase` in
`FractalDataWorks.Web.Clients.Abstractions`, which provides:

- Automatic bearer token attachment via `BearerTokenHandler`
- Structured error logging via `ClientLog`
- Authentication failure detection (401/403)

## Authentication

Cookie-based Blazor Server authentication. On login, the server exchanges credentials with
`Reference.Api` (`/api/v1/auth/token`), parses the JWT into a `ClaimsPrincipal`, and stores
the access/refresh tokens in a secure HttpOnly cookie. `OnValidatePrincipal` handles
token refresh transparently before expiry.

## ctrl/cfg Schema Split (FDW 0.9.7+)

Configuration items sourced from the **ctrl** schema (system-seeded, read-only) carry
`IsSystem = true`. Headless providers expose this flag on item contexts. Pages must render
system items as read-only (no edit/delete actions). See the main [README](../README.md) for details.
