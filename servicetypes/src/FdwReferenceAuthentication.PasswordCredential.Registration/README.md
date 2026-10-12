# PasswordCredential

Proves a caller by the password they hold, against this platform's own credential store.

The one step that both proves and identifies. Every federated step splits those in two — an
authority proves a subject, and `ResolvePrincipal` binds that subject to a principal — because the
authority does not know our principals. Here the credential is checked against a row that already
*is* the principal, so a binding step has nothing to look up.

| | |
|---|---|
| **Requires** | nothing |
| **Contributes** | `Subject`, `Principal` |
| **AuthenticationMethods** | `["pwd"]` (RFC 8176 — a knowledge factor) |

The password is read from `IPasswordCredentialAccessor` at the moment of verification and never
contributed to the context: a credential on the context is a credential every later step can read
and anything logging the context can print.

Whatever received the credential presents it, so the step never knows the transport: the HTTP login
endpoint and a SQL Server login each open a `PresentedPasswordCredentialScope` around the flow they
run, and the step reads it through `PresentedPasswordCredentialAccessor`.

## Registering it

Referencing this package is the whole registration — `[Implementation(typeof(AuthenticationStepTypes),
"PasswordCredential")]` on `PasswordCredentialStepType` self-registers via a module initializer.
This package registers `PresentedPasswordCredentialAccessor`, as itself and as
`IPasswordCredentialAccessor`, in its own `Registration` phase — a host that never references this
package never pulls it in.

## Using it

```json
{ "Name": "PasswordLogin", "Audience": "reference-api", "ExecutionLifetime": "00:15:00",
  "Steps": ["PasswordCredential", "BakePermissions", "AuthorizeIssuance"] }
```

Seeded as `PasswordLogin` in `databases-seed/ConfigurationDb/seed/28-seed-password-login-flow.sql`.

## Building another step

This package follows the `FdwReferenceAuthStep` dotnet template
(`reference-servicetypes/public/templates/FdwReferenceAuthStep`) — instantiate it for the full
authoring guide (what `Requires`/`Contributes` mean, what the runner enforces, how a step reaches the
current request). Domain reference: `fdw-anatomy` → **authentication** → *The shipped steps*.
