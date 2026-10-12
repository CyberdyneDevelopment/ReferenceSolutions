# Service domain with configuration providers

A `ServiceTypeCollection` domain with the two-tier configuration provider pattern every FDW domain
is built on: a **domain provider** that reads the domain row and dispatches by `Implementation`, and
one **implementation provider** per option that reads that implementation's own row.

## What's generated

```
Commands/SampleDomainConfigurationCommand.cs    names the domain table
SampleDomainConfigurationProvider.cs            domain provider — DomainConfigurationProviderBase
SampleDomainTypeBase.cs                         base every implementation option closes
SampleDomainTypes.cs                            the ServiceTypeCollection; registers the domain provider
Abstractions/
  ISampleDomainType.cs                          marker for this collection's members
  ISampleDomainImplementationConfiguration.cs   the contract every implementation satisfies
  ISampleDomainConfigurationProvider.cs         the domain provider's own interface

SampleImplementation/                           ONE example implementation — SAMPLE_OPTION
  SampleImplementationConfiguration.cs          every field this implementation reads at runtime
  Commands/SampleImplementationConfigurationCommand.cs
  ISampleImplementationConfigurationProvider.cs the implementation provider's own interface
  SampleImplementationConfigurationProvider.cs  ImplementationProviderBase over SAMPLE_CATEGORY.SampleImplementation
  SampleImplementationType.cs                   [Implementation] — registers its provider, then
                                                registers it into the domain provider
  Logging/SampleImplementationLog.cs
```

## The domain/implementation split

A configured member is two rows. The **domain row** is the platform's one `DomainConfiguration`
record — Id, Name, Domain and the Implementation that selects it, and nothing else — so this domain
declares no record of its own, only the table it lives in (`SAMPLE_CATEGORY.SAMPLE_CATEGORY`). The
**implementation row** lives in the implementation's own table and carries every field its factory
reads; a field put on the domain row would read as empty after dispatch, because dispatch returns the
implementation, not the domain row. Name, Domain and Implementation are stamped onto the
implementation by the domain provider on every read and never persisted there.

Neither provider has logic in it. Each supplies a store, a schema and a table to its base.

## Registration, and why the lifetimes matter

`SampleDomainTypes` adds the domain configuration provider **first**, then runs each option's own
Register. `SampleImplementationType` adds its implementation provider in Registration and registers it
into the domain provider in Initialization — correct because the domain configuration provider is a
**singleton**, so root's one Initialize call reaches the instance everything later resolves.

## What this template does NOT cover

This is the **configuration** layer: the domain provider resolves configuration, not a running
service. A domain that also constructs runtime services additionally:

- declares `ServiceInterface`, `ProviderType` and `ProviderInterface` on the `[ServiceTypeCollection]`
  attribute in `SampleDomainTypes.cs`
- adds its **scoped** service provider in `SampleDomainTypes`' Registration body, beside the
  configuration provider and before the option loop
- gives each implementation option a real `TService`/`TFactory`, an implementation service provider,
  and a Registration body that **decorates** the domain's scoped `ServiceDescriptor` — removing it and
  re-adding a factory that registers the option into the instance whichever scope builds. Registering
  into a scoped provider from Initialize reaches only root's instance, and every request then fails
  with "No factory registered for service option type".

`NotificationServiceTypes` (FDW) and `EmailNotificationType` (reference-servicetypes) are that layer,
working.

## Adding a second implementation

Copy `SampleImplementation/` to a new folder, give it its own `Implementation` string, its own table,
and its own `[Implementation]`. Nothing in `SampleDomainTypes.cs` changes — the collection gathers what
attached itself.

## Registering it

`[Implementation]` and `[ServiceTypeCollection]` are the whole registration. Reference the package and
both the domain and its implementation are live; remove the reference and every `Implementation`
lookup naming SAMPLE_OPTION fails rather than quietly finding nothing.
