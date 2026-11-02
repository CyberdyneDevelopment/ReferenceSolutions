# Password

Sign-in type: password login. Brings in the steps a `PasswordLogin` flow needs, so a host
references one package instead of three.

No code of its own — same rule as every step package: **referencing the package is the
registration**. This one just references what a password flow needs:

| Package | Contributes |
|---|---|
| `FdwReferenceAuthentication.PasswordCredential.Registration` | `Subject`, `Principal` — proves and identifies in one step |
| `FdwReferenceAuthentication.BakePermissions.Registration` | `Claims` — what the principal may do |
| `FdwReferenceAuthentication.AuthorizeIssuance.Registration` | `Decision` — the permit-or-deny gate |

## Using it

```json
{ "Name": "PasswordLogin", "Audience": "reference-api", "ExecutionLifetime": "00:15:00",
  "Steps": ["PasswordCredential", "BakePermissions", "AuthorizeIssuance"] }
```

Seeded as `PasswordLogin` in `databases-seed/ConfigurationDb/seed/28-seed-password-login-flow.sql`.

A host that wants `TenantSwitch` too still references
`FdwReferenceAuthentication.TenantSwitch.Registration` directly — it shares `BakePermissions` and
`AuthorizeIssuance` with this bundle (both packages can be referenced together without conflict),
but is not itself part of the `Password` sign-in type.

See also: `fdw-anatomy` → **authentication** → *The shipped steps*, *Building a step*.
