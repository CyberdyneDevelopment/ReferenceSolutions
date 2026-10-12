# Reference stack templates

`dotnet new` templates for the reference stack. They live beside the code they generate against, so
a change to an endpoint group's shape and the template that emits it move in the same commit.

| Short name | What it emits |
|---|---|
| `fdw-ref-api-host` | An API host: `configurationSchema.json`, the three-phase `PlatformServices` lifecycle, authentication and forwarded headers. Prompts for the database connection and its authentication type. |
| `fdw-ref-endpoint-group` | An endpoint group package: the group collection, its `EndpointBase`, one endpoint and the `[TypeOption]` that declares it. |

Each generated project carries a `README.md` explaining the part of the system it belongs to — the
authentication flow for the host, the anatomy of an endpoint for the group.

## Generating

```bash
dotnet pack public/templates/FdwReferenceTemplates.csproj -c Release -o ./pkg
dotnet new install ./pkg/FdwReference.Templates.*.nupkg

dotnet new fdw-ref-api-host -n MyApi --AuthenticationType ManagedIdentity --DbName MyConfigDb
dotnet new fdw-ref-endpoint-group -n Widget --RoutePrefix widgets
```

`dotnet new <template> --help` lists every prompt with its choices.

## References are packages, not projects

Generated projects use `<PackageReference>` with no version, because they are generated into a repo
that manages versions centrally. That is deliberate: a template that pinned a version would go stale
the first time the base moved, and one that used `<ProjectReference>` would only work inside this
repo.

## Verifying

```bash
./verify-templates.sh
```

Generates from every template and **compiles the output**. A template that packs and installs
cleanly can still emit code that does not build; this is the only check that catches it.
