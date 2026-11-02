# AuthorizeIssuance

The permit-or-deny gate. Decides whether a token may be issued for the principal established
earlier in the flow.

Last in a flow, because it decides on what everything before it established.

| | |
|---|---|
| **Requires** | `Principal` |
| **Contributes** | `Decision` |
| **AuthenticationMethods** | `[]` — deciding is not proving; nothing about the caller is established here |

Delegates the actual eligibility check to `IIssuanceEligibility` — a member of the tenant the
principal was resolved into, or a member of the global tenant. A refusal here is the last line of
defense before a token is minted, so it is a hard failure, not a warning.

## Registering it

Referencing this package is the whole registration.

## Using it

The last step of every shipped flow:

```json
{ "Name": "PasswordLogin", "Steps": ["PasswordCredential", "BakePermissions", "AuthorizeIssuance"] }
{ "Name": "AuthentikLogin", "Steps": ["OidcRedirect", "ResolvePrincipal", "BakePermissions", "AuthorizeIssuance"] }
```

## Building another step

Follows the `FdwReferenceAuthStep` dotnet template
(`reference-servicetypes/public/templates/FdwReferenceAuthStep`) — instantiate it for the full
authoring guide. Domain reference: `fdw-anatomy` → **authentication** → *The shipped steps*.
