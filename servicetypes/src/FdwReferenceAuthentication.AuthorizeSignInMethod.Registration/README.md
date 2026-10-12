# AuthorizeSignInMethod

Refuses issuance to a principal who does not hold the permission a flow names. This is how an
operator decides, per sign-in method, who may use it.

| | |
|---|---|
| **Requires** | `Principal`, `Claims` (so it runs after `BakePermissions`) |
| **Contributes** | `Decision`, and only ever a refusal |
| **AuthenticationMethods** | `[]` |
| **Configuration** | `{ "RequiredPermission": "signin:password" }` |

| Operator wants | Configure |
|---|---|
| Nobody may use the method | Set the flow's `IsEnabled` to 0 |
| Every active user may use it | Leave this step out of the flow |
| Only some users may use it | Add this step after `BakePermissions`, and grant those users a role carrying the permission |

A refusal stands: `AuthorizeIssuance` running later cannot overrule it. Malformed settings, or
settings with no `RequiredPermission`, make the flow invalid when it loads.

## Registering it

Referencing this package is the whole registration. The `Password` and `AuthentikSso` sign-in types
bring it in.

## Using it

The seeded `PasswordLogin` flow's step rows (`auth.AuthenticationFlowStepConfiguration`):

| StepOrder | StepName | Configuration |
|---|---|---|
| 1 | `PasswordCredential` | |
| 2 | `BakePermissions` | |
| 3 | `AuthorizeSignInMethod` | `{"RequiredPermission":"signin:password"}` |
| 4 | `AuthorizeIssuance` | |
