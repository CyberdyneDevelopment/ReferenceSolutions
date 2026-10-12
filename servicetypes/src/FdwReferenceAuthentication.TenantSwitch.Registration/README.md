# TenantSwitch

Establishes the caller of a tenant switch: the principal already authenticated on the request, now
asking to act in another tenant.

A user acts in one tenant at a time — the tenant their token was issued for — and switching is
asking for a new token for another tenant (FDW-825). This step contributes only who is asking and
where to; it decides nothing. Whether they may act there is `AuthorizeIssuance`'s ordinary
eligibility check: a member of that tenant, or a member of the global tenant. What the new token
carries is `BakePermissions`' ordinary answer — the roles held in *that* tenant.

| | |
|---|---|
| **Requires** | nothing |
| **Contributes** | `Subject`, `Principal` |
| **AuthenticationMethods** | `[]` — no credential is verified here, so no method is proved |

The identity is the one the caller already proved: it comes from the token the request was
authenticated with, read via `ITenantSwitchRequestAccessor`, never from the request body.

## Registering it

Referencing this package is the whole registration. It also owns `HttpTenantSwitchRequestAccessor`
and registers it, along with `IHttpContextAccessor`, in its own `Registration` phase.

## Using it

Not seeded yet — no `auth.AuthenticationFlow` row names this step in any of the four seeds under
`databases-seed/ConfigurationDb/seed/`. To wire it up, add a flow shaped like the others:

```json
{ "Name": "TenantSwitch", "Audience": "reference-api", "ExecutionLifetime": "00:15:00",
  "Steps": ["TenantSwitch", "BakePermissions", "AuthorizeIssuance"] }
```

## Building another step

Follows the `FdwReferenceAuthStep` dotnet template
(`reference-servicetypes/public/templates/FdwReferenceAuthStep`) — instantiate it for the full
authoring guide. Domain reference: `fdw-anatomy` → **authentication** → *The shipped steps*.
