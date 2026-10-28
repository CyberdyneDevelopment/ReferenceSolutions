# SampleGroup.Endpoints — the anatomy of an endpoint

Four files, and each one exists for a reason. The one people leave out is the third.

```
SampleGroupEndpointOptions/
  SampleGroupEndpoints.cs        the GROUP        joins EndpointGroups, and hosts options
  SampleGroupEndpointBase.cs     the BASE         what an option in this group is
  ListSampleGroupOption.cs       the DECLARATION  puts the route on the wire
ListSampleGroupEndpoint.cs       the HANDLER      the code that runs
SampleGroupSummaryDto.cs         the CONTRACT     what goes over the wire
```

## Three ways to generate it

`dotnet new fdw-ref-endpoint-group --Permission widgets --Mode <Plain|Base|Seal> --Operation <List|Read|Create|Update|Delete>`

| Mode | Makes | Use it when |
|---|---|---|
| `Plain` (default) | The group and one plain FastEndpoints endpoint with its right stated. | You write the endpoint's handling yourself. |
| `Base` | The group and the open-generic abstract endpoint for the operation: a Crud base with the resource name, the OpenAPI summary, description and tag stated, and its generics left open. No concrete endpoint, so no option. | You are building the group's shared base in the package that owns the data. |
| `Seal` | The group and a concrete class (not marked `sealed`) that closes the generics of the Crud base for the operation, with its constructor, summary, tag and option. Every member the base leaves abstract fails with a declared message (`SampleGroupEndpointLog.NotWired`) until you connect it to the service that owns the data. | You are putting an endpoint on the wire from a base. To close a base of your own, change the base type and its using. |

`--Permission` is the resource name of a Crud endpoint: it is the route segment and the prefix of the right the base gates on (`widgets:read`, `widgets:write`, `widgets:delete`). The event id in `SampleGroupEndpointLog` (a `Seal` file) is the template's: pick the next free one in your package's range.

The OpenAPI document shows each route's summary, description and tag. The Crud bases take them from `EndpointSummary`, `EndpointDescription` and `EndpointTag`, which the `Base` and `Seal` files state.

## The group's own API reference

Every mode also generates the group's reference page, `/api/v1/<Permission>/scalar`, and the document it reads, `GET /openapi/<Permission>`, with the option that declares it. Like the Data and Sign-in pages, the page is for a signed-in caller and the document is cut per caller: it lists only the operations the caller holds the right for, with the summary, description and tag each endpoint states. A Crud endpoint is routed at the Permission, so the document keeps the paths under it; a Plain endpoint is routed at its own route prefix, which the document also keeps.

## The declaration is the route table

**Endpoint auto-discovery is off.** Nothing scans your assembly for classes that look like
endpoints. A route exists because a `[TypeOption]` declared it, and for no other reason.

This is the failure mode worth memorising: write the handler, forget the option, and everything
compiles. Tests that call the handler directly pass. The route returns **404** — not 401, not 500
— because as far as the host is concerned it was never declared. There is nothing to grep for and
no error to read.

Two attributes doing different jobs, and it is worth being precise about which is which:

```csharp
[TypeOption(typeof(EndpointGroups), "SampleGroupEndpoints")]        // joins the group to the registry
[TypeCollection(typeof(SampleGroupEndpointBase), …)]                // lets options join THIS group
public partial class SampleGroupEndpoints : EndpointTypeCollectionBase<SampleGroupEndpointBase>
```

Membership, not parent/child — because a partial class cannot span assemblies, and endpoint groups
routinely do.

## How an option reaches the collection

Options attach themselves at assembly load through a module initializer. A collection **gathers**
options that have attached; it never sweeps for them. Two consequences:

- **Referencing the package is the intent to have its endpoints.** Not referencing it is how you
  decline them. There is no configuration switch.
- A group whose package is never referenced registers nothing, silently. Its endpoints 404. This
  is not hypothetical — four identity operations in the reference API return 404 today for exactly
  this reason: the host references neither the old package nor the new one that replaced it.

A `PackageVersion` entry in `Directory.Packages.props` is central version management, **not** a
reference. Only `<PackageReference>` in the consuming csproj registers anything.

## Why the handler is thin

An endpoint is an HTTP orchestrator: validate, call a service, map to a DTO, respond. It does not
own data access and it does not own business logic. If the same operation would be wanted from a
background job or another endpoint, it belongs in a service.

The DTO is a separate type from whatever the service returns. That boundary is what lets the
service change shape without breaking the wire contract, and it is why the DTO lives in this
package rather than being borrowed from a domain package.

## Adding a second endpoint

Add two files, not one:

1. `MyThingEndpoint.cs` — the handler.
2. `SampleGroupEndpointOptions/MyThingOption.cs` — `[TypeOption(typeof(SampleGroupEndpoints), "MyThing")]`.

Then check the route actually appears. The cheapest verification is the running host's own
document: if the operation is absent from `/api/v1/openapi/reference` fetched as a platform admin, it is
not registered, and no amount of correct handler code will change that.

## Namespaces follow folders

`IDE0130` is enforced, so a file's namespace must match its folder relative to the project
root. What this template emits already satisfies it:

```
SampleGroup.Endpoints/ListSampleGroupEndpoint.cs
    namespace SampleGroup.Endpoints

SampleGroup.Endpoints/SampleGroupEndpointOptions/ListSampleGroupOption.cs
    namespace SampleGroup.Endpoints.SampleGroupEndpointOptions
```

The options folder carrying its own namespace segment is not decoration — it is what keeps
the rule and the group structure agreeing. Add a folder, add the matching segment.

**Where the rule and the framework disagree.** A TypeCollection normally wants its
collection, base, interface and options in ONE namespace, because that cohesion is what
makes `using` a single import and keeps a family readable. When such a family spans folders
— or spans assemblies, which it must whenever a partial class cannot reach across a package
— `IDE0130` will flag every file that is not in a folder named after its namespace.

Suppress the rule for those projects instead, and say why in the suppression. The namespace
is the deliberate choice; the folder is the accident.

**A namespace that names a package which no longer exists is a different problem** — that is
staleness, and it is worth fixing. It happens after a consolidation: the code moves into a
new assembly and the namespace keeps naming the old one. The give-away is a namespace whose
first segment matches no project in the solution.

## Which package an endpoint belongs in

An endpoint group belongs to **one host**. Decide which before you create the package,
because the answer is an assembly boundary and not a folder.

```
the host that does the work        gets the real endpoint
the host that forwards to it       gets a proxy, in its OWN package
```

**A proxy and the endpoint it proxies declare the same route.** That is correct — the API
host answering `POST /etl/executions/{id}/approve` by forwarding to the ETL host, which
answers the same path and owns the queue. Same URL, different host, and the client cannot
tell.

It stops being correct the moment both live in one assembly. Then both options register
against the same route in the same host, **registration order decides which one answers**,
and nothing in the code says so. You do not get an error; you get whichever the module
initializer reached first.

This is not hypothetical. A package consolidation put twelve endpoint packages into one
assembly here, two of which were a proxy and its service:

```
POST /etl/executions/{id}/approve    ApproveExecutionEndpoint        (does the work)
                                     ApproveEtlExecutionProxyEndpoint (forwards)
POST /etl/trigger/{type}             UnifiedTriggerEndpoint          (does the work)
                                     TriggerOrchestrationNodeProxyEndpoint
```

Both compiled, both registered, and the route silently belonged to whichever won the race.
The fix was to split the assembly by host, not to rename anything — the namespaces had
been saying where the code belonged the whole time.

Two rules fall out of it:

- **Never put a proxy in the package that holds the endpoint it proxies.** If you are
  writing a `*ProxyEndpoint`, it wants its own package, referenced only by the forwarding
  host.
- **A telling sign you are in the wrong package:** your endpoint takes `IHttpClientFactory`
  and its neighbours take a service or a provider. One of you is forwarding and the other
  is doing the work, and you are not the same layer.

## Verbs

`GET` lists only. `POST` creates, and also carries single reads that need a body. `PATCH` modifies.
`PUT` is not used: it replaces the whole resource, so a partial update cannot say "not supplied". A modify DTO uses nullable properties so "not supplied" and "set to empty" stay
distinguishable — collapsing those two is how a partial update silently blanks a field.
