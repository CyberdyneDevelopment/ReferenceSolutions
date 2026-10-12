# AuthentikSso

Sign-in type: Authentik SSO. Brings in the steps an `AuthentikLogin` flow needs, so a host
references one package instead of four.

No code of its own — same rule as every step package: **referencing the package is the
registration**. This one just references what an Authentik-federated flow needs:

| Package | Contributes |
|---|---|
| `FdwReferenceAuthentication.OidcRedirect.Registration` | `Subject`, `Claims` — proves the caller against Authentik |
| `FdwReferenceAuthentication.ResolvePrincipal.Registration` | `Principal` — binds the proved subject to a local one, provisioning JIT if configured |
| `FdwReferenceAuthentication.BakePermissions.Registration` | `Claims` — what the principal may do |
| `FdwReferenceAuthentication.AuthorizeIssuance.Registration` | `Decision` — the permit-or-deny gate |

## Using it

```json
{ "Name": "AuthentikLogin", "Audience": "reference-api", "MinimumAcr": "urn:cyberdyne:acr:single-factor",
  "ExecutionLifetime": "00:15:00",
  "Steps": ["OidcRedirect", "ResolvePrincipal", "BakePermissions", "AuthorizeIssuance"] }
```

Seeded as `AuthentikLogin` in `databases-seed/ConfigurationDb/seed/31-seed-authentik-login-flow.sql`,
with its provisioner binding in `30-seed-claim-mapped-auto-admin-provisioner.sql`.

A different OIDC provider is the same shape — `OidcRedirect`'s mechanism (authorization code +
PKCE) is provider-agnostic; only the issuer/endpoint configuration differs, which lives in
`auth.OidcAuthority` rows, not in code.

See also: `fdw-anatomy` → **authentication** → *Federated login*, *JIT provisioning*, *Building a step*.
