# ManagementUI-Tailwind

Blazor Server + Tailwind CSS management interface for the FractalDataWorks enterprise architecture.

## Architecture

This is a **Headless UI Skin** built with .NET Blazor Server and Tailwind CSS. It decouples rendering from business logic by consuming **Protocol Providers** from the core framework.

```
┌─────────────────────────────────────────────────────────────────┐
│                     ManagementUI-Tailwind                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │  Blazor      │  │  Tailwind    │  │  Headless Providers  │   │
│  │  Server      │  │  CSS v4      │  │  (Core logic)        │   │
│  └──────┬───────┘  └──────┬───────┘  └──────────┬───────────┘   │
│         └─────────────────┴─────────────────────┘               │
│                           ▼                                      │
│                    ┌──────────────────────────┐                  │
│                    │ Per-Domain API Clients   │                  │
│                    │ (.Clients packages)      │                  │
│                    └──────────┬───────────────┘                  │
│                               ▼                                  │
│                        ┌─────────────┐                           │
│                        │  REST API   │                           │
│                        └─────────────┘                           │
└─────────────────────────────────────────────────────────────────┘
```

### Key Pattern: Logic-Lite Skin

Pages in this solution do not make direct API calls. Instead, they wrap their content in a **Logic Provider** (e.g. `UserProvider`, `ConnectionProvider`) which handles the state, loading indicators, and error management. This allows the Tailwind solution to focus 100% on the visual design and user experience.

## Configuration Model (FDW 1.3.0)

FDW 1.3.0 ships a single user-writable schema (`cfg`) inside **ConfigurationDb**. The old `ctrl` schema and dual-source provider pattern have been removed. The startup minimum needed to reach ConfigurationDb (its own connection details, container/field/key metadata, secret-manager declarations) ships in `public/configurationSchema.json` and is loaded into `IConfiguration` at the start of `Program.cs`. Every other connection and configuration record is runtime data inside ConfigurationDb.

The UI never queries ConfigurationDb directly; it goes through the per-domain `.Clients` packages against Reference.Api endpoints.

### Write path

All configuration mutations go through `IConfigurationWriter<T>` (top-level configs: Connection, DataStore, DataSet, Pipeline, Schedule, Settings, Role, SecretManager) or `IDynamicConfigurationWriter` (generic admin UI dynamic types). `ConfigurationSaveCommand<T>` via DataGateway is used only for child config records.

## Tech Stack

| Layer | Technology |
|-------|------------|
| Framework | .NET 10 Blazor Server |
| Language | C# |
| Styling | Tailwind CSS 4 (standalone CLI) |
| Icons | Inline SVG (Heroicons) |
| Theme | Cyberdyne Dark (custom) |

**No npm required** - Uses the standalone `tailwindcss.exe` for CSS compilation.

## Pages

| Page | Route | Description |
|------|-------|-------------|
| Dashboard | `/` | System overview with metrics |
| Pipelines | `/pipelines` | Pipeline list with status |
| Pipeline Builder | `/pipelines/new` | Visual drag-drop editor |
| Connections | `/connections` | Database connection management |
| DataStores | `/datastores` | Physical storage locations |
| DataSets | `/datasets` | Logical dataset definitions |
| Schedules | `/schedules` | Cron-based job scheduling |
| Calculations | `/calculations` | Formula library |
| Lineage | `/lineage` | Data lineage visualization |
| Dataflow | `/dataflow` | ETL execution monitor |
| Mapper | `/mapper` | Schema field mapping |
| Data Preview | `/data-preview` | Query and preview data |
| Audit | `/audit` | Execution history |
| Settings | `/settings` | User preferences |
| Login | `/login` | Authentication |

## Quick Start

```bash
# Prerequisites: .NET 10 SDK, API running on port 5001

cd ManagementUI-Tailwind
dotnet run

# Opens https://localhost:5010
```

## Project Structure

```
ManagementUI-Tailwind/
├── Components/
│   ├── App.razor              # Root component
│   ├── Routes.razor           # Router
│   ├── _Imports.razor         # Global usings
│   ├── Layout/
│   │   ├── MainLayout.razor   # Main shell with sidebar
│   │   ├── NavItem.razor      # Navigation link component
│   │   └── EmptyLayout.razor  # Layout for login page
│   └── Pages/
│       ├── Home.razor         # Dashboard
│       ├── Connections.razor
│       ├── DataStores.razor
│       ├── DataSets.razor
│       ├── Schedules.razor
│       ├── Calculations.razor
│       ├── Lineage.razor
│       ├── Dataflow.razor
│       ├── Mapper.razor
│       ├── DataPreview.razor
│       ├── Audit.razor
│       ├── Settings.razor
│       ├── Login.razor
│       └── Pipelines/
│           ├── Index.razor    # Pipeline list
│           └── Builder.razor  # Visual editor
│
├── Styles/
│   └── input.css              # Tailwind source CSS
│
├── wwwroot/
│   ├── css/app.css           # Compiled Tailwind CSS
│   └── favicon.png
│
├── tailwindcss.exe           # Standalone Tailwind CLI
├── Program.cs
├── appsettings.json
└── ManagementUI-Tailwind.csproj
```

## Theming

The UI uses the **Cyberdyne theme** - a dark, industrial design inspired by sci-fi aesthetics:

| Element | Color | CSS Variable |
|---------|-------|--------------|
| Background | Deep navy/black | `--background: 224 71% 4%` |
| Primary | Terminator red | `--primary: 0 84% 60%` |
| Accent | Electric cyan | `--accent: 199 89% 48%` |
| Text | Cold white | `--foreground: 213 31% 91%` |

### Theme Features

- No rounded corners (brutalist design)
- Monospace font throughout
- Grid background pattern
- Red glow effects on buttons
- Animated status indicators

See [THEMING.md](THEMING.md) for customization.

## Comparison with MudBlazor UI

| Feature | MudBlazor UI | Tailwind UI |
|---------|--------------|-------------|
| Component Library | MudBlazor | Custom (Tailwind) |
| Styling | MudBlazor Theme | Tailwind CSS |
| Theme | Modern/Material | Cyberdyne Dark |
| Dependencies | MudBlazor NuGet | tailwindcss.exe |
| Complexity | More abstraction | Closer to HTML/CSS |

Both UIs share:
- Same REST API backend
- Same functionality
- .NET Blazor Server runtime

## Development

```bash
# Build and run
dotnet run

# Build with CSS watch (in separate terminal)
./tailwindcss.exe -i Styles/input.css -o wwwroot/css/app.css --watch

# Build for production
dotnet publish -c Release
```

## CSS Component Classes

The Tailwind input CSS defines these reusable classes:

| Class | Purpose |
|-------|---------|
| `.btn-primary` | Primary red button |
| `.btn-cyber` | Outlined button with glow hover |
| `.btn-outline` | Gray outlined button |
| `.btn-ghost` | Transparent button |
| `.card` | Dark bordered container |
| `.card-header` | Card header with title |
| `.card-content` | Card body |
| `.input` | Text input field |
| `.table-container` | Table wrapper |
| `.badge-*` | Status badges |
| `.nav-item` | Sidebar navigation link |

## Required Services

| Service | URL | Purpose |
|---------|-----|---------|
| Reference.Api | https://localhost:5001 | Backend API |

## License

Part of the FractalDataWorks Reference Solutions.
