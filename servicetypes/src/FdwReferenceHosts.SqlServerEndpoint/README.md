# SQL Server endpoint (FDW-1021)

A host that references `FdwReferenceHosts.SqlServerEndpoint.Registration` listens for SQL Server
clients over TDS. A client signs in with a SQL login, and sees one database. Each workspace the
login may read is a schema in it, and each data set attached to a workspace is a table
(`BASE TABLE`). The database is read-only: writes and DDL are refused with error 3906.

## Turning it on

1. Reference `FdwReferenceHosts.SqlServerEndpoint.Registration` from the host. The host must also
   register the workspace endpoints group, which registers `IWorkspaceReadService`, the service
   every table is read through. It must also register the PasswordCredential authentication step.
2. Declare the `sqe` path in the host's `configurationSchema.json`, under the
   `ServerConfiguration` data store's `Paths`. `host-setup/configurationSchema.sqe-path.json` is
   the block to paste. The SELECT columns come from this declaration, so it must match the rows.
3. Add the rows: copy `host-setup/server-config/sqe/` to the host's `server-config/sqe/` and fill
   in every `<...>` value. Every setting is required. The listener checks them all when the host
   starts, and a row that is missing or unusable stops the host with the setting named.

| Setting | What it is |
|---|---|
| `ListenAddress`, `Port` | Where the listener binds. 1433 is SQL Server's own port. |
| `ServerName` | What `@@SERVERNAME` and `SERVERPROPERTY('ServerName')` answer. |
| `ProductVersion`, `Edition`, `EngineEdition` | The SQL Server presented. 16.x is SQL Server 2022; engine edition 2 is Standard. |
| `Collation`, `Language` | `SQL_Latin1_General_CP1_CI_AS` or `Latin1_General_CI_AS`; `us_english`. |
| `DatabaseName` | The one database. A login that names no database, or this one, opens it. `master` opens master. Any other is error 4060. |
| `LoginFlowName` | The authentication flow a SQL login runs. It must run the PasswordCredential step and deliver a session (`SessionServiceName` set). |
| `SecretManagerName`, `CertificateSecretName`, `CertificatePasswordSecretName` | Where the TLS certificate is held. |
| `MaximumMessageBytes` | The largest batch or RPC a signed-in caller may send; at least 32768, one full TDS packet. Pre-login and LOGIN7 are read against their own smaller bounds. |
| `MaximumConnections`, `LoginTimeoutSeconds` | Limits on what an unauthenticated client can make the server do. |

## The certificate

Every connection is encrypted. A client that cannot encrypt is refused, because LOGIN7 carries
the password. The certificate is a PKCS#12 file with its private key, stored base64-encoded in one
secret, with its password in another:

```bash
openssl req -x509 -newkey rsa:3072 -nodes -days 825 -subj "/CN=sql.example.com" \
  -addext "subjectAltName=DNS:sql.example.com" -keyout sql.key -out sql.crt
openssl pkcs12 -export -inkey sql.key -in sql.crt -out sql.pfx   # prompts for the password
base64 -w0 sql.pfx > sql.pfx.b64   # this text is the CertificateSecretName secret's value
```

For a vendor, use a certificate their machines trust, issued for the name they connect to.
Microsoft's newer drivers encrypt by default and check the certificate. A self-signed certificate
works only with "Trust server certificate" (`TrustServerCertificate=True`).

## Testing with SSMS

- Server name: `host,port` (a comma, not a colon), e.g. `sql.example.com,1433`.
- Authentication: SQL Server Authentication, with a login the flow accepts.
- Encryption: Mandatory or Strict both work. Tick "Trust server certificate" for a self-signed
  certificate.
- Options → Connect to database: `DatabaseName`, or leave it as default.
- Start in a query window: `SELECT * FROM INFORMATION_SCHEMA.TABLES`, then
  `SELECT TOP 10 * FROM <workspace>.<data set>`.
- Object Explorer runs many catalog queries on connect. Any it uses that this endpoint does not
  answer show up as errors in its tree. Those errors are the list of what to add next. They don't
  affect the query window.

## What a query can do

- `SELECT` from one data set's table, with `WHERE` sent to the store. A condition the store
  cannot run is refused, never filtered afterwards. `ORDER BY`, `OFFSET`/`FETCH` and `TOP` are sent
  to a store that pages. `ORDER BY` on a store that does not order is refused. Aggregates
  (`COUNT`, `SUM`, `AVG`, `MIN`, `MAX`) and `DISTINCT` run over the rows returned.
- `SELECT` over catalog views (`sys.tables`, `sys.columns`, `sys.objects`, `sys.schemas`,
  `sys.types`, `sys.databases`, `INFORMATION_SCHEMA.TABLES`/`COLUMNS`/...), with joins between
  them. Also expressions and the usual built-in functions (`SERVERPROPERTY`, `DB_NAME`,
  `OBJECT_ID`, `ISNULL`, `CAST`, `CONVERT`, ...).
- Variables, `SET` options, `USE`, `IF`, `BEGIN...END`, `PRINT`, transactions (bookkeeping only;
  nothing writes), `EXEC`, and the RPCs .NET, ODBC and JDBC drivers send: `sp_executesql`,
  `sp_prepare`, `sp_execute`, `sp_prepexec`, `sp_unprepare`.
- Not yet: `GROUP BY`, `UNION`, CTEs, joins that involve a data set's table, and a data set
  column's decimal precision (it is taken from the values; `SET FMTONLY ON` refuses such a column).
