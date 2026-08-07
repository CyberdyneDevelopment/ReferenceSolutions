# Changelog

All notable changes to Reference UI will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.9.6] - 2026-03-17

### Changed

- **FDW dependency** — bumped to `0.9.6-Build.7` (adds ctl schema Connection/DataStore support via SchemaOverride + IndexOffset).

## [0.9.5] - 2026-03-16

### Changed

- **FDW dependency** — bumped to `0.9.5-rc.35`.
- **`FdwLegacyClientsVersion` removed** — the `FdwLegacyClientsVersion` property in `Directory.Packages.props` has been removed as it is no longer used.

### Removed

- **`FractalDataWorks.UI.Components.Blazor`** — remove this package reference and replace with `FractalDataWorks.UI.Components`. See the FDW 0.9.5 changelog for the full migration guide.
- **`FractalDataWorks.UI.Blazor.MudBlazor`** — remove this package reference and move any MudBlazor theme infrastructure directly into the application.
- **`FractalDataWorks.UI.Blazor.Tailwind`** — remove this package reference. Schema display is now headless via `FractalDataWorks.Schema.Components`.
