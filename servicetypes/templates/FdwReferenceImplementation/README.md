# ImplName ServiceName — a reference implementation

Two projects, deliberately:

| Project | Holds | Referencing it gets you |
|---|---|---|
| `FdwReferenceServiceNamePlural.ImplName` | the **aggregation** — the service, its factory and factory interface | the composition, and **no** DI registration |
| `FdwReferenceServiceNamePlural.ImplName.ServiceType` | the **registration** — the `[Implementation]` | the registration **and** the aggregation, transitively |

## Why they are separate

**A package reference IS a registration.** A `[Implementation]` registers itself through a module
initializer at assembly load, so referencing a package that contains one enlists the whole service
domain whether you wanted it or not.

Splitting them gives the consumer the choice. Someone who only wants to *use* the service — construct
it, call it, test it — references the aggregation and pays for none of the domain wiring. Someone who
wants the service registered references the service-type package and gets both.

The dependency only ever points one way: **ServiceType → aggregation**. Never the reverse. If your
aggregation needs something from the service-type project, the split will not hold — that is a real
signal, not an obstacle to work around.

## Why the namespace is shared

Both projects declare the same namespace. Only the owning assembly differs. That keeps every
fully-qualified name stable, so the `[Implementation]`'s FNV-1a `Id` — derived from the FQN — does
not move and no persisted configuration needs rewriting. A consumer hitting `CS0246` after this split
needs a **package reference**, not a code edit.

## Rules worth not relearning

- **No `InternalsVisibleTo`.** A reference implementation is a third-party consumer by construction.
  Anything needed to compose the service must be public API; if it is not, that is a gap in FDW to
  fix, not a rule to waive.
- **Reference every source generator the code needs.** Generators emit per-compilation. A missing one
  makes the generated members silently vanish and the partial fails with `CS8795`.
- **The entry point needs `Fdw.Registration.SourceGenerators`.** Cross-assembly `[TypeOption]`s
  register via a module initializer emitted in the entry-point compilation. Without it the
  collections read empty off a perfectly clean build.
- **Never split one `TypeCollection` across two packages.** It is complete only when both are
  referenced — a runtime failure with no build-time signal.
- **No fallbacks.** `logger ?? NullLogger<T>.Instance` is the only permitted `??` default. A missing
  domain value fails loud with a MessageLogging-backed result.

## Usage

```bash
dotnet new fdw-ref-implementation \
  --ImplName Sqlite \
  --ServiceName SecretManager \
  --ServiceNamePlural SecretManagers
```

| Parameter | Meaning | Example |
|---|---|---|
| `ImplName` | the kind being implemented | `Sqlite`, `MsSql`, `AzureKeyVault` |
| `ServiceName` | the domain, singular | `SecretManager`, `Connection` |
| `ServiceNamePlural` | the FDW package/namespace segment | `SecretManagers`, `Connections` |

`ServiceName` and `ServiceNamePlural` are separate because FDW packages are named for the plural
(`Fdw.Services.SecretManagers`) while the types are named for the singular
(`SecretManagerTypeBase`). Getting one from the other by adding an "s" is wrong often enough to be
worth asking.

## What builds, and what you must finish

**`FdwReferenceServiceNamePlural.ImplName` (the aggregation) builds clean as generated.** It is
deliberately self-contained: it takes no dependency on the domain's contracts, so you get a
compiling starting point.

**`FdwReferenceServiceNamePlural.ImplName.ServiceType` does NOT build until you close it on your
domain's real types**, and that is not a defect in the template — FDW's domains genuinely differ:

- `ServiceNameTypeBase<,,>` constrains its type arguments to the domain's own contracts, so
  `ImplNameServiceName` must implement the domain's service interface, and
  `IImplNameServiceNameFactory` its factory interface (`CS0311` until it does).
- There is no registration method to override. An option sets its phase bodies in its constructor —
  `Registration((builder, loggerFactory) => ...)` and `Initialization((host, loggerFactory) => ...)` —
  and what differs per domain is what goes in them: the domain's provider interfaces, and their
  lifetimes. A **scoped** domain service provider is decorated from Registration, wrapping the
  domain's own `ServiceDescriptor`; a **singleton** configuration provider is registered into from
  Initialization. The TODO in `ImplNameServiceNameType.cs` shows both, and `EmailNotificationType`
  is the working example.

Open the real base in `Fdw.Services.ServiceNamePlural` and let the compiler drive. A template that
guessed these would be wrong for most domains and, worse, wrong in a way that looks right.

`ImplNameServiceNameConfiguration` is a placeholder so the pair compiles standalone. A real
implementation configuration implements its domain's `I<Domain>ImplementationConfiguration`
contract, lives in its own table joined to the domain row, and carries every field the factory
reads — the domain row holds only its Id, Name, Domain and Implementation.

## Add a MessageLogging class

The template ships none, because the generated shape is easy to get subtly wrong. Copy any
`Fdw.Services.*/Logging/*Log.cs`: a `[MessageLoggingTypeCode("PREFIX")]` static partial class whose
methods are `static partial IGenericMessage` taking `ILogger` plus at least one parameter referenced
by a `{placeholder}` in the message.

FDW code logs through these generated methods, never raw `ILogger` calls, because the method returns
the `IGenericMessage` that a failing `IGenericResult` carries — the log line and the failure are the
same object. A log method and its ResultCode share a number, and the number is chosen by **meaning**,
deliberately reused across prefixes for the same logical event. Do not hunt for a free slot.
