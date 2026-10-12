# fdw-ref-scaffold

A menu-driven wizard for `dotnet new fdw-ref-api-host`, so the template's Platform Configuration
connection, secret manager, ServerConfiguration root and additional DataStores don't have to be
recalled as bare `--Flag value` pairs.

## Install

```bash
dotnet tool install -g FdwReferenceScaffold.Tools
```

`fdw-ref-scaffold` and the `fdw-ref-api-host` template are separate NuGet installs (a dotnet tool
and a template package) — installing this tool does **not** install the template at the package
level. But generating is `fdw-ref-scaffold`'s whole job, so before it generates it checks whether
the template is there and, if not, installs `FdwReference.Templates` itself. No manual step needed.

## Use

```bash
fdw-ref-scaffold
```

Pick a menu item to answer that group of questions; answers persist across menu visits.

- **Platform Configuration connection** — the one connection every fdw-ref-api-host has: where it
  reads its own configuration from, and how it authenticates. The authentication choices are read
  off the connection type's own FDW TypeCollection, so a method FDW adds or changes shows up here
  without an edit.
- **Other connections** — additional named DataStores the app will use beyond ServerConfiguration
  and PlatformConfiguration. The template has no symbol for a variable-length connection list, so
  these are patched into the generated `configurationSchema.json` after `dotnet new` runs, not
  passed as `--Flag` arguments.
- **Host configuration** — the secret manager the host registers (which also decides the
  `ReferenceSecretManagers.*.Registration` package the generated csproj references) and the
  ServerConfiguration root directory.

**Review and generate** shows the effective `dotnet new` command line and, on confirmation, runs
it. Afterward, for every MsSql connection whose server/database no longer look like placeholders
and whose password (where the authentication method needs one) resolves from `FDW_SECRET_<key>` in
the process environment, the wizard attempts a real connection and — on success — writes the
introspected schema straight into that DataStore's `Paths` array. A connection that can't be
reached is skipped with a reason, never a failure; PostgreSql, Sqlite and FileSystem connections
aren't introspected.

## Relationship to raydar-scaffold

This is a reverse port of `Raydar.Scaffold.Tools` from the raydar developer kit. The raydar
template carries 42 symbols to this one's 14, so the external-identity, chain-mapped-roles, tenant
and solution-naming steps have no counterpart here and are not present — a flag `fdw-ref-api-host`
does not declare makes `dotnet new` fail rather than being ignored. Package versions are likewise
absent by design: templates in this repo take them from `Directory.Packages.props`, and
`verify-templates.sh` fails the build if a template pins its own.
