# Authentication step

One step of an authentication flow. A flow is an ordered list of step names; the runner walks it,
and each step contributes to a shared context until there is enough to issue a token.

## What you must decide

**What it contributes.** `Subject` if it proves who someone is, `Principal` if it resolves one
locally, `Claims` if it gathers facts, `Decision` if it decides whether a token may be issued. The
runner discards anything you produce outside this declaration and logs it — the declaration is
enforced, not documentation.

**What it requires.** Checked when the flow's configuration loads, so ordering a step before its
requirement fails at startup naming what is missing, rather than at 3am on someone's login.

**Its authentication method**, if it proves anything — an RFC 8176 value like `pwd`, `otp`, `hwk`.
The runner records this once the step has succeeded, and derives the assurance level from every
method recorded. Declare only what the step actually enforces: a step claiming `otp` without
checking one makes every downstream assurance decision a lie, and no later step can repair that.

## What you cannot do

Set your own `amr` or `acr` — those have no public setter, and the compiler will stop you.
Contribute something you did not declare — the runner drops it. Reach the issued token — it is not
in the context. These are structural, so a mistake here is a compile error or a discarded value
rather than a privilege escalation.

## Registering it

The `[Implementation]` attribute is the whole registration. Reference the package and the step is
selectable; remove the reference and every flow naming it fails at startup.

## Using it

Add its name to a flow, in order:

```json
{ "Name": "MyLogin", "Audience": "my-api", "MinimumAcr": "urn:cyberdyne:acr:multi-factor",
  "Steps": [ "ForeignToken", "SampleStep", "ResolvePrincipal", "AuthorizeIssuance" ] }
```
