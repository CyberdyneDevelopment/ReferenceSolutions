# SampleApi — the authentication flow

This host issues its own tokens. Every request that reaches an endpoint carries one this host
minted, whether the caller proved who they were with a password, an agent key, or an external
identity provider. That is the shape worth holding on to: **one issuer, several ways to earn a
token**, rather than several authentication systems running side by side.

## Earning a token

```
POST /auth/login
  X-Forwarded-Proto: https        REQUIRED — see "the scheme" below
  Content-Type: application/json

  { "username": "admin", "password": "…" }
```

A correct password returns 200 with `{ "accessToken", "tokenType": "Bearer", "expiresAt" }`. A
wrong one returns **401 with no body detail** — **that is the healthy failure**, and it means the
credential was actually checked and rejected. Which of "no such user", "wrong password" or
"account disabled" occurred is in the log for an operator, and deliberately out of the response so
it cannot be used to enumerate accounts.

An HTTP **500** means the login flow itself is misconfigured — the endpoint could not even reach
the point of checking a credential — and it does not tell you which of several things stopped it.
Read the journal for the first ERR line and let it name the cause. Two seen in practice: the
AuthDb connection could not be built because its password was absent, and a signing key that
could not be resolved. They present identically at the endpoint.

## Where the password actually lives

Split across two databases, deliberately.

```
usr.Users.Salt          ConfigurationDb    the per-user salt
auth.UserSecret.Hash    AuthDb             the stored hash
sec.Secret              ConfigurationDb    the pepper, via the secret manager
```

```
derived = PBKDF2-HMAC-SHA512(password, salt, 210_000 iterations, 32 bytes)
stored  = Base64(HMAC-SHA256(pepper, derived))
```

Compared with `CryptographicOperations.FixedTimeEquals`. Reading one database is not enough to
mount an offline attack, and the pepper is never in either of them.

**The credential is verified exactly once, by the token manager.** The split is by *authority*,
not by layer: either an external provider vouched for the caller and no password ever reaches us,
or we verify it ourselves. One authority, one check.

## External identity is an exchange, not a pass-through

An external provider's token is never the token your endpoints see. It is validated once, the
principal is provisioned to a local user, and **this host mints its own token** for the session.
So downstream services trust exactly one issuer, and revoking access is a local operation.

## The signing key

Resolved **once, at startup**, in the token manager's Initialization phase — not lazily on first
request. A host that cannot resolve its key **does not start**, and the failure names the
`auth.TokenManager` row and the secret it could not read.

If you find an older note describing an exception thrown from
`OpenIddictServerFactory.CreateTransactionAsync`, that shape is gone. It was the reason a missing
key surfaced as a 500 on every login instead of a boot failure, which made a valid user, a bogus
user and a wrong password indistinguishable.

## The scheme, and why the header is not optional

The traffic chain terminates TLS before your app:

```
browser --https--> Cloudflare --https--> nginx --http--> Caddy --http--> this host
```

The app therefore sees `http` unless something tells it otherwise. The platform reads the forwarded
headers for you — there is nothing to add to `Program.cs`. What remains yours is the other half: the
proxy actually sending `X-Forwarded-Proto`, which a preview-slot Caddy route does **not** do by
default and must be PATCHed per slot.

Get it wrong and the symptom is not an error. `Request.Scheme` returns `http`, the auth cookie is
issued `Secure` over what the client believes is a plain connection, the client discards it, and
the next request is anonymous — a login that reports success and then bounces back to the login
page. The server log will say `signed in`. Believe the cookie, not the log line.

## Authorization, once the token is in hand

Permissions are `resource:action` strings baked into the token at issuance, not looked up per
request.

```
authz.Permission        the vocabulary
authz.Role              named bundles
authz.RolePermission    which role grants what
authz.UserRole          who holds which role
authz.SystemRoleMapping names the platform's Admin / Operator / Viewer roles
```

Because they are baked in, a permission change takes effect on the **next** token, not the
current one. That is the trade for not querying the grant tables on every request.

## Session context and tenancy

`MsSqlSessionContextTypes` is a total partition — `SystemContext` / `ForUser` / `Deny`.

`SystemContext` is **explicit elevation**, not the anonymous case. It is the one option that
leaves `SESSION_CONTEXT('UserId')` unset, which `security.fn_TenantFilter` reads as full
visibility. Every other option writes a real Guid or the reserved deny principal, so an
unauthenticated caller lands on `Deny` and **can never reach the bypass by omission**. Reading it
the other way round inverts the security model.

## Bringing up a new environment

The failures below are the ones that actually happen, in the order they bite:

1. `sec.Secret` has no signing-key row → the host does not start, and says so.
2. `auth.UserSecret` has no row for the user → 401, correctly.
3. The proxy does not send `X-Forwarded-Proto` → login "succeeds" and nothing sticks.

---

# configurationSchema.json — setting up the connections

This file declares only what the host needs to **reach** its configuration. That is two connections:

| Connection | Kind | Holds |
|---|---|---|
| `ServerConfiguration` | FileSystem, always | this host's own boot-time rows — logging, telemetry, authentication services, the host record |
| `PlatformConfiguration` | chosen at generation | the shared store — connections, secret managers, users, everything a domain reads at run time |

`ServerConfiguration` is files because it is read first. The gateway builds it before it opens
`PlatformConfiguration`, and logging reads its own configuration from it, so it has to be reachable
with no database and no secret. The directory named by `Root` must hold those rows;
`reference-api`'s `server-config/` is the worked example of what goes in it.

Every other connection — OpsDb, your data stores, anything a user configures — is runtime data
inside `PlatformConfiguration` and is never written here. Nor is a secret manager: FDW's
`ConfigurationSchema` has no `SecretManagers` section, and a secret manager is configured in
`PlatformConfiguration` like any other domain member.

The names matter. Each domain reads from the connection its collection names in
`ConfigurationConnection`. Logging, Telemetry, Authentication and DataGateway default to
`ServerConfiguration`; most other domains default to `PlatformConfiguration`. `HostTypes` defaults to
`PlatformConfiguration`, so a host that keeps its Host row in `ServerConfiguration`, as `reference-api`
does, sets `HostTypes.ConfigurationConnection` before registration runs. Rename a connection here and
nothing finds it unless those properties are set to match.

## Filled in at generation

`dotnet new fdw-ref-api-host` asks which kind of store `PlatformConfiguration` is, then asks only for
what that kind needs:

```
--ConnectionType     MsSql | PostgreSql | Sqlite | FileSystem
--ServerConfigRoot   the ServerConfiguration directory, default server-config
--SecretManager      EnvironmentVariable | MsSql — which secret manager option is registered
```

**MsSql, PostgreSql** — server-backed, so it asks for the server:

```
--DbServer           the ConfigurationDb host name or IP (default localhost)
--DbName             the catalogue
--DbPort             1433
--DbUser             the login
--AuthenticationType SqlAuth | ManagedIdentity | EntraId | AzureCli | WindowsAuth
--SecretKeyName      logical name of the password       (SqlAuth only)
```

**Sqlite, FileSystem** — file-backed, so it asks for one thing:

```
--ConfigPath         the SQLite file (DataSource), or the configuration directory (Root)
```

The generated `configurationSchema.json` contains only the keys that apply, and the generated
`.csproj` references only the registration packages for what was chosen: the FileSystem connection
always, the `PlatformConfiguration` kind, and the secret manager. Referencing a `*.Registration`
package is what registers its option — the package without the suffix registers nothing — and the
gateway resolves each connection's factory from the kind the schema declares, so a kind with no
registration package has no factory and the host does not start. `dotnet new fdw-ref-api-host --help`
lists every prompt.

Everything below is what to change afterwards, when the answer turns out to be different.

## Only SqlAuth has a password

That is the whole rule, and it decides the shape of the connection entry.

**SqlAuth** — needs `Username`, a `SecretManagerName` and a `SecretKeyName`. The password itself is
never in this file; the named secret manager resolves it:

```json
"AuthenticationType": "SqlAuth",
"AdditionalProperties": {
  "Username": "fdw_config",
  "SecretManagerName": "EnvSecrets",
  "SecretKeyName": "CONFIG_PASSWORD"
}
```

Check this against a running host before relying on it. A secret manager is configured in
`PlatformConfiguration`, so this asks for the `PlatformConfiguration` password through a manager that
is read out of the database it is trying to open. `reference-api` reaches its platform store with
`WindowsAuth` (API-133).

**ManagedIdentity, EntraId, AzureCli, WindowsAuth** — no password exists, so **delete the whole
`AdditionalProperties` block**. The identity comes from the platform, not from a secret:

```json
"AuthenticationType": "WindowsAuth",
"TrustServerCertificate": true
```

Leaving an unused `SecretManagerName` behind is worse than untidy: it names a manager the connection
will try to resolve, and a manager that cannot be resolved stops the host — for a secret nothing was
ever going to read.

## Choosing the secret manager

```
EnvironmentVariable   reads secrets from the process environment
MsSql                 reads sec.Secret from PlatformConfiguration
```

MsSql is for the secrets used *after* the platform store is reachable, such as signing keys and
outbound credentials. It cannot supply the password of the connection it is read through.

## Values that resolve at startup

None. The schema names its server directly (`--DbServer`, default `localhost`), and an FDW host reads
no configuration from environment variables. The loader still replaces `${NAME}` from the environment
when a value contains one; do not use it.

## What failure looks like

| Symptom | Cause |
|---|---|
| Host does not start, naming `ServerConfiguration` | The `Root` directory is not there, or `FdwReferenceConnections.FileSystem.Registration` is not referenced. |
| Host does not start, naming a connection kind | That kind's `*.Registration` package is not referenced, so it has no factory. |
| Host does not start, naming a secret | The secret manager resolved but the key is absent. |
| Host does not start, naming a connection | `${…}` unresolved, or the server is unreachable. |
| `A required authentication property is absent or empty` | The auth type declares a property this file did not supply — a `SqlAuth` connection with no `Username` or no secret. |
| Startup is fine, every request 500s | The connection is fine; something later is not. Read the first ERR line rather than assuming this file. |
