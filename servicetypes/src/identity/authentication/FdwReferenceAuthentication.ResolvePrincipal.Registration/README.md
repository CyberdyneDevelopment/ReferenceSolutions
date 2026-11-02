# ResolvePrincipal

Binds a subject an authority proved to a principal this platform knows — provisioning one
just-in-time when a deployment has configured that for the subject's issuer.

The half of federated login that the authority cannot do: it proved who the caller is to *itself*,
and has no idea which of our principals that corresponds to. Every federated step therefore
contributes a `Subject` and stops; this step turns that into a `Principal`. It stays a step of its
own rather than folding into each authority's step, because the mapping is the same wherever the
subject came from.

| | |
|---|---|
| **Requires** | `Subject` |
| **Contributes** | `Principal` |
| **AuthenticationMethods** | `[]` — resolution proves nothing; the subject was already proved |

An unbound subject is not automatically a refusal: `IExternalIdentityProvisionerBindingConfigurationProvider`
is consulted for a provisioner bound to the subject's issuer, scoped to the global tenant. No
binding row is the default-OFF outcome and refuses exactly as before — provisioning an account for
a caller this platform has never decided to trust is not something an absence of configuration
should do. A bound issuer hands the subject and every claim contributed so far to that provisioner,
which decides what (if anything) to create; this step never inspects claim content itself.

## Registering it

Referencing this package is the whole registration — no accessor of its own, unlike
`PasswordCredential`/`TenantSwitch`.

## Using it

```json
{ "Name": "AuthentikLogin", "Audience": "reference-api", "MinimumAcr": "urn:cyberdyne:acr:single-factor",
  "ExecutionLifetime": "00:15:00",
  "Steps": ["OidcRedirect", "ResolvePrincipal", "BakePermissions", "AuthorizeIssuance"] }
```

Seeded as `AuthentikLogin` in `databases-seed/ConfigurationDb/seed/31-seed-authentik-login-flow.sql`,
with its provisioner binding in `30-seed-claim-mapped-auto-admin-provisioner.sql`.

## Building another step

Follows the `FdwReferenceAuthStep` dotnet template
(`reference-servicetypes/public/templates/FdwReferenceAuthStep`) — instantiate it for the full
authoring guide. Domain reference: `fdw-anatomy` → **authentication** → *The shipped steps*,
*JIT provisioning*.

## Redeeming invitations (FDW-910)

With no Configuration, the step resolves bound subjects and leaves an unbound one to provisioning,
as before. To let an operator pre-create an account and have the person's first sign-in bind to it,
give the step row settings:

```json
{ "Invitations": { "EmailClaim": "email", "EmailVerifiedClaim": "email_verified" } }
```

For an issuer that sends no verification claim but controls its users' addresses (Entra ID), use
`"TrustIssuerEmail": true` instead of `EmailVerifiedClaim`. Exactly one of the two is required.

An unbound subject whose issuer asserted exactly one address, verified, redeems the open invitation an
operator wrote for that issuer and address (`POST /users/{IdOrName}/external-identities` with
`invitedEmail`). The invitation becomes the binding and is never matched by address again. An
invitation comes before provisioning, so the invited person never gets a second account.
