# Reference.Nfl

POCO record library for the `nfl.*` tables on the reference DataDb. Records are marked `[GenerateMapper]` so the FDW data source generators produce IDataReader/IDataRecord mappers for use with `IDataGateway` commands. No services, no DI registration — pure data shape.

- **TargetFramework:** `net10.0`
- **SDK:** `Microsoft.NET.Sdk`
- **Namespace:** `Reference.Nfl`

## Records (`NflRecords.cs`)

| Type | Maps to |
|------|---------|
| `TeamRecord` | `nfl.Team` |
| `PlayerRecord` | `nfl.Player` |
| `GameRecord` | `nfl.Game` |
| `PlayerGameStatRecord` | `nfl.PlayerGameStat` |
| `SeasonStandingRecord` | `nfl.SeasonStanding` |

Each record uses `{ get; set; }` POCO properties matching SQL column names exactly and is decorated with `[ExcludeFromCodeCoverage]` + `[GenerateMapper]`.

## Dependencies

From `Reference.Nfl.csproj`:

- `FractalDataWorks.Data.Abstractions`
- `FractalDataWorks.Data.SourceGenerators` (analyzer; generates the mappers)
- `FractalDataWorks.Registration.SourceGenerators` (analyzer; emits module initializer to register the mappers)

## Consumed by

`Reference.Api` references this project for its NFL endpoints (`Endpoints/Nfl/`).
