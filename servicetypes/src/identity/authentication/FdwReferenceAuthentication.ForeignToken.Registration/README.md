# ForeignToken

Accepts a token an external authority already issued (RFC 8693 token exchange), verifies it
against that authority's published keys, and turns it into a `Subject`.

The mechanism — verify a JWT against an issuer's keys — is the same for Auth0, Entra or Authentik;
only the issuer, audiences and algorithms differ. That decision (which authority, on what terms) is
deployment-specific, which is why this step lives here rather than in the framework. Claims a
foreign token carries are the authority's assertions, not this platform's, so they arrive marked
`ClaimSources.External` and change no decision without an explicit mapping.

| | |
|---|---|
| **Requires** | nothing |
| **Contributes** | `Subject`, `Claims` |
| **AuthenticationMethods** | asserted by the foreign token, read after `Execute` returns |

## Registering it

Referencing this package is the whole registration. A host that never references it cannot name
`ForeignToken` in a flow — the intended outcome, not an oversight.

## Using it

Pair with `ResolvePrincipal` to bind the verified subject to a local principal:

```json
{ "Name": "TokenExchange", "Steps": ["ForeignToken", "ResolvePrincipal", "BakePermissions", "AuthorizeIssuance"] }
```

Not currently seeded — `ForeignAuthority` rows configure which issuers this deployment trusts.

## Building another step

Follows the `FdwReferenceAuthStep` dotnet template
(`reference-servicetypes/public/templates/FdwReferenceAuthStep`) — instantiate it for the full
authoring guide. Domain reference: `fdw-anatomy` → **authentication** → *The shipped steps*,
*Federated login*.
