# OidcRedirect

Sends a caller to an OIDC provider (authorization code + PKCE, RFC 9700) and turns what comes back
into a `Subject`.

Runs twice. On the first call it has no authorization code, so it returns a `Challenge`: the
destination and a single-use resume token the runner stores alongside everything established so
far. When the caller returns, the callback resumes the flow, this step exchanges the code, and
verifies the token it gets back.

The mechanism is identical for Auth0, Entra or Authentik — only the issuer, endpoints, audiences and
algorithms differ, which is why this step lives here rather than in the framework.

How the host authenticates at the provider's token endpoint is declared on the authority row
(`auth.OidcAuthorityConfiguration`) the way a connection declares its authentication: a
`ClientAuthenticationType` plus a key/value set in the child table `auth.OidcAuthorityProperty`
(`AdditionalProperties` on the row). The type is required; a blank or unknown one is refused and no
default is assumed.

| `ClientAuthenticationType` | Client | Properties | Code exchange |
|---|---|---|---|
| `None` | Public (Entra with PKCE) | none | PKCE only; no `client_secret` is sent |
| `ClientSecret` | Confidential (Authentik) | `SecretManagerName`, `SecretKeyName`, both required | Also sends `client_secret` |

The property values are names, never secret values. `SecretManagerName` is the secret manager
(`MsSqlSecrets` in the seeds) and `SecretKeyName` is the key under which it holds the secret. The step
reads the secret for each exchange and keeps it nowhere. A provider that requires a secret refuses an
exchange without one with `invalid_client`, and the caller sees `access_denied`.

| | |
|---|---|
| **Requires** | nothing |
| **Contributes** | `Subject`, `Claims` |
| **AuthenticationMethods** | asserted by the provider, read after `Execute` returns |

## Registering it

Referencing this package is the whole registration. A host that never references it cannot name
`OidcRedirect` in a flow — the intended outcome, not an oversight.

## Using it

```json
{ "Name": "AuthentikLogin", "Audience": "reference-api", "MinimumAcr": "urn:cyberdyne:acr:single-factor",
  "ExecutionLifetime": "00:15:00",
  "Steps": ["OidcRedirect", "ResolvePrincipal", "BakePermissions", "AuthorizeIssuance"] }
```

Seeded as `AuthentikLogin` in `databases-seed/ConfigurationDb/seed/31-seed-authentik-login-flow.sql`.

## Building another step

Follows the `FdwReferenceAuthStep` dotnet template
(`reference-servicetypes/public/templates/FdwReferenceAuthStep`) — instantiate it for the full
authoring guide. Domain reference: `fdw-anatomy` → **authentication** → *The shipped steps*,
*Federated login*.
