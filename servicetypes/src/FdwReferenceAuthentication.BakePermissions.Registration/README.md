# BakePermissions

Resolves what the established principal may do and states it as claims for the token to carry.

| | |
|---|---|
| **Requires** | `Principal` |
| **Contributes** | `Claims` |
| **AuthenticationMethods** | `[]` — resolving permissions proves nothing |

Reads permissions for the principal's tenant as a trusted, system-context read (see
`PermissionBakingLog` and `AuthenticationContextAccessor`'s system scope) — the caller has not yet
been granted anything at the point this step runs, so it cannot read its own permissions under its
own authority.

## Registering it

Referencing this package is the whole registration.

## Using it

Runs near the tail of nearly every flow, after whichever step established the principal:

```json
{ "Name": "PasswordLogin", "Steps": ["PasswordCredential", "BakePermissions", "AuthorizeIssuance"] }
{ "Name": "AuthentikLogin", "Steps": ["OidcRedirect", "ResolvePrincipal", "BakePermissions", "AuthorizeIssuance"] }
```

Seeded in both `28-seed-password-login-flow.sql` and `31-seed-authentik-login-flow.sql`.

## Building another step

Follows the `FdwReferenceAuthStep` dotnet template
(`reference-servicetypes/public/templates/FdwReferenceAuthStep`) — instantiate it for the full
authoring guide. Domain reference: `fdw-anatomy` → **authentication** → *The shipped steps*.
