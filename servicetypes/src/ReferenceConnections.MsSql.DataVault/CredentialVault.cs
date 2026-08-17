using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Credentials.Abstractions;
using Fdw.Services.Credentials.Abstractions.Outcomes;
using ReferenceCredentials.Sql.Logging;
using Fdw.Services.Credentials.Sql.Outcomes;
using ReferenceConnections.MsSql.DataVault;
using Microsoft.Extensions.Logging;

using ReferenceCredentials.Sql;
using ReferenceConnections.MsSql;

namespace ReferenceConnections.MsSql.DataVault;

/// <summary>
/// Sealed SQL Server credential vault. One vault, three narrow interfaces:
/// <see cref="ICredentialVault"/> (passwords over <c>auth.UserSecret</c>), <see cref="IPatVault"/>
/// (personal access tokens over <c>auth.PersonalAccessToken</c>), and <see cref="IAgentKeyVault"/>
/// (agent keys over <c>auth.AgentKey</c>). Every verb uses the inherited parameterized primitives
/// (<c>Query</c>/<c>NonQuery</c>/<c>QueryRows</c>) plus <c>Pepper</c>/<c>ConstantTimeEquals</c>; the
/// stored value is always <c>Base64(HMAC(secret, pepper))</c>. No verb returns hash/salt/token bytes.
/// </summary>
/// <remarks>
/// <para>
/// The pepper is the vault's single secret (resolved once in system context). Passwords arrive
/// already KDF-derived at the edge (plaintext never enters the vault); PAT/agent-key raw values are
/// minted at the edge and passed in solely to be peppered here. The negative validation path runs the
/// SAME constant-time compare against a fixed decoy so timing does not enumerate accounts (README §6).
/// </para>
/// <para>
/// DDL FOLLOW-UP (databases repo): <c>auth.UserSecret.Salt</c> and <c>auth.UserSecret.AlgorithmName</c>
/// are <c>NOT NULL</c> in the shipped schema but the salt/algorithm now live at the edge (README §4/§8);
/// password Create/Change store only the peppered <c>Hash</c>, so those two columns must be made
/// nullable (or dropped) for Create/Change to run at runtime.
/// </para>
/// </remarks>
public sealed class CredentialVault : MsSqlDataVaultBase, ICredentialVault, IPatVault, IAgentKeyVault
{
    // Why: passwords are stored under this discriminator on auth.UserSecret; one current row per user.
    private const string PasswordSecretType = "Password";

    // Why: a fixed 32-byte (HMAC-SHA-256 sized) value compared against on the negative path so the
    // constant-time compare runs identically whether or not a secret exists (anti-enumeration, README §6).
    private static readonly byte[] DecoyHash = new byte[32];

    // Why: the vault produces ONLY the two compare outcomes. The framework does not auto-register
    // cross-assembly plain TypeOptions at runtime (only ServiceTypeCollections), so the vault returns
    // the canonical option instances directly rather than via CredentialOutcomes.ByName — the
    // CredentialOutcomes collection remains the type contract for the (edge-composed) policy outcomes.
    private static readonly ICredentialOutcome MatchOutcomeValue = new MatchOutcome();
    private static readonly ICredentialOutcome NoMatchOutcomeValue = new NoMatchOutcome();

    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialVault"/> class with its already-resolved
    /// connection and pepper (the provider resolves both once in system context).
    /// </summary>
    /// <param name="vaultName">The vault's name.</param>
    /// <param name="connection">The resolved data connection the vault rides.</param>
    /// <param name="pepper">The resolved pepper (HMAC key) bytes.</param>
    /// <param name="logger">Optional logger.</param>
    public CredentialVault(
        string vaultName,
        [ServiceOptionDependency] IDataConnection connection,
        byte[] pepper,
        ILogger<CredentialVault>? logger = null)
        : base(vaultName, connection, pepper, logger)
    {
    }

    // ====================================================================================
    // ICredentialVault — passwords over auth.UserSecret (derived hashes only; never plaintext)
    // ====================================================================================

    /// <inheritdoc />
    public async Task<IGenericResult<ICredentialOutcome>> Validate(Guid userId, byte[] derivedHash, CancellationToken cancellationToken = default)
    {
        if (derivedHash is null || derivedHash.Length == 0)
            return GenericResult<ICredentialOutcome>.Failure(CredentialVaultLog.DerivedHashMissing(Logger, "Validate"));

        CredentialVaultLog.ValidateStarted(Logger, userId);

        var stored = await Query<string>(
            "SELECT Hash FROM auth.UserSecret WHERE UserId=@u AND SecretType=@t AND IsCurrent=1 AND IsDeleted=0",
            cancellationToken, ("@u", userId), ("@t", PasswordSecretType)).ConfigureAwait(false);
        if (!stored.IsSuccess)
            return stored.ToNewResult<ICredentialOutcome>();

        var candidate = Pepper(derivedHash);

        // Why: §6 — run the SAME compare whether or not a secret is on file; the decoy keeps timing uniform.
        byte[] comparand;
        if (string.IsNullOrEmpty(stored.Value))
        {
            CredentialVaultLog.NoSecretOnFile(Logger, userId);
            comparand = DecoyHash;
        }
        else
        {
            comparand = DecodeOrDecoy(stored.Value);
        }

        var outcome = ConstantTimeEquals(candidate, comparand) ? MatchOutcomeValue : NoMatchOutcomeValue;

        CredentialVaultLog.ValidateCompleted(Logger, userId, outcome.Name);
        return GenericResult<ICredentialOutcome>.Success(outcome);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Create(Guid userId, byte[] derivedHash, CancellationToken cancellationToken = default)
    {
        if (derivedHash is null || derivedHash.Length == 0)
            return GenericResult.Failure(CredentialVaultLog.DerivedHashMissing(Logger, "Create"));

        var write = await VersionOnWriteSecret(userId, Convert.ToBase64String(Pepper(derivedHash)), cancellationToken).ConfigureAwait(false);
        if (!write.IsSuccess)
            return write;

        CredentialVaultLog.SecretCreated(Logger, userId);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Change(Guid userId, byte[] oldDerivedHash, byte[] newDerivedHash, CancellationToken cancellationToken = default)
    {
        if (oldDerivedHash is null || oldDerivedHash.Length == 0 || newDerivedHash is null || newDerivedHash.Length == 0)
            return GenericResult.Failure(CredentialVaultLog.DerivedHashMissing(Logger, "Change"));

        var stored = await Query<string>(
            "SELECT Hash FROM auth.UserSecret WHERE UserId=@u AND SecretType=@t AND IsCurrent=1 AND IsDeleted=0",
            cancellationToken, ("@u", userId), ("@t", PasswordSecretType)).ConfigureAwait(false);
        if (!stored.IsSuccess)
            return stored;

        // Why: verify the current secret first; reject (fail loud) if absent or mismatched.
        var comparand = string.IsNullOrEmpty(stored.Value) ? DecoyHash : DecodeOrDecoy(stored.Value);
        if (string.IsNullOrEmpty(stored.Value) || !ConstantTimeEquals(Pepper(oldDerivedHash), comparand))
            return GenericResult.Failure(CredentialVaultLog.ChangeRejectedOldMismatch(Logger, userId));

        var write = await VersionOnWriteSecret(userId, Convert.ToBase64String(Pepper(newDerivedHash)), cancellationToken).ConfigureAwait(false);
        if (!write.IsSuccess)
            return write;

        CredentialVaultLog.SecretChanged(Logger, userId);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Disable(Guid userId, CancellationToken cancellationToken = default)
    {
        var res = await NonQuery(
            "UPDATE auth.UserSecret SET IsCurrent=0, IsDeleted=1, ModifyDate=sysdatetimeoffset() " +
            "WHERE UserId=@u AND SecretType=@t AND IsCurrent=1 AND IsDeleted=0",
            cancellationToken, ("@u", userId), ("@t", PasswordSecretType)).ConfigureAwait(false);
        if (!res.IsSuccess)
            return res;

        // Why: disable is idempotent — no current secret is the desired end state, so log a warning
        // but still succeed rather than fail loud on an already-absent secret.
        if (res.Value == 0)
            CredentialVaultLog.NoCurrentSecretToDisable(Logger, userId);
        else
            CredentialVaultLog.SecretDisabled(Logger, userId);

        return GenericResult.Success();
    }

    // Why: version-on-write in a single atomic batch — retire any current row then insert the new
    // current row. The peppered hash is stored Base64-encoded in the NVARCHAR Hash column. Salt and
    // AlgorithmName are NOT written (they live at the edge) — see the DDL follow-up note above.
    private async Task<IGenericResult> VersionOnWriteSecret(Guid userId, string base64Hash, CancellationToken cancellationToken)
    {
        var res = await NonQuery(
            "SET XACT_ABORT ON; BEGIN TRANSACTION; " +
            "UPDATE auth.UserSecret SET IsCurrent=0, ModifyDate=sysdatetimeoffset() " +
            "WHERE UserId=@u AND SecretType=@t AND IsCurrent=1 AND IsDeleted=0; " +
            "INSERT auth.UserSecret (Id, UserId, SecretType, Hash, IsCurrent, IsDeleted) " +
            "VALUES (NEWID(), @u, @t, @h, 1, 0); " +
            "COMMIT TRANSACTION;",
            cancellationToken, ("@u", userId), ("@t", PasswordSecretType), ("@h", base64Hash)).ConfigureAwait(false);
        if (!res.IsSuccess)
            return res;

        if (res.Value < 1)
            return GenericResult.Failure(CredentialVaultLog.WriteAffectedNoRows(Logger, userId, "StoreSecret"));

        return GenericResult.Success();
    }

    // Why: stored peppered hashes are Base64 of 32 HMAC-SHA-256 bytes. A corrupt/short value can never
    // equal a real candidate; falling back to the 32-byte decoy keeps the compare constant-time.
    private static byte[] DecodeOrDecoy(string base64)
    {
        try
        {
            return Convert.FromBase64String(base64);
        }
        catch (FormatException ex)
        {
            // Why: corrupt Base64 → decoy hash for constant-time comparison. ex is observed.
            _ = ex;
            return DecoyHash;
        }
    }

    // ====================================================================================
    // IPatVault — personal access tokens over auth.PersonalAccessToken
    // ====================================================================================

    /// <inheritdoc />
    public async Task<IGenericResult<Guid>> Create(Guid userId, string rawToken, string label, DateTime? expiresAt, int maxActiveTokens, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return GenericResult<Guid>.Failure(PersonalAccessTokenLog.ValidationFailed(Logger));

        // Why: enforce the per-user active-token limit atomically before inserting (mechanism); the
        // edge supplies the limit value (policy).
        var count = await Query<int>(
            "SELECT COUNT(*) FROM auth.PersonalAccessToken WHERE UserId=@u AND IsRevoked=0",
            cancellationToken, ("@u", userId)).ConfigureAwait(false);
        if (!count.IsSuccess)
            return count.ToNewResult<Guid>();

        if (count.Value >= maxActiveTokens)
            return GenericResult<Guid>.Failure(PersonalAccessTokenLog.TokenLimitReached(Logger, userId, maxActiveTokens));

        var tokenId = Guid.NewGuid();
        var insert = await NonQuery(
            "INSERT auth.PersonalAccessToken (Id, UserId, Name, TokenHash, ExpiresAt, IsRevoked) " +
            "VALUES (@id, @u, @n, @h, @e, 0)",
            cancellationToken,
            ("@id", tokenId), ("@u", userId), ("@n", label),
            ("@h", PepperSecret(rawToken)),
            ("@e", expiresAt.HasValue ? new DateTimeOffset(expiresAt.Value, TimeSpan.Zero) : (object?)null)).ConfigureAwait(false);
        if (!insert.IsSuccess)
            return insert.ToNewResult<Guid>();

        if (insert.Value < 1)
            return GenericResult<Guid>.Failure(CredentialVaultLog.WriteAffectedNoRows(Logger, userId, "CreateToken"));

        PersonalAccessTokenLog.TokenCreated(Logger, userId, tokenId);
        return GenericResult<Guid>.Success(tokenId);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<PersonalAccessTokenValidationResult>> Validate(string rawToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return GenericResult<PersonalAccessTokenValidationResult>.Failure(PersonalAccessTokenLog.ValidationFailed(Logger));

        var rows = await QueryRows(
            "SELECT Id, UserId, IsRevoked, ExpiresAt FROM auth.PersonalAccessToken WHERE TokenHash=@h",
            r => new TokenRow(
                (Guid)r["Id"]!,
                (Guid)r["UserId"]!,
                (bool)r["IsRevoked"]!,
                r["ExpiresAt"] as DateTimeOffset?),
            cancellationToken, ("@h", PepperSecret(rawToken))).ConfigureAwait(false);
        if (!rows.IsSuccess)
            return rows.ToNewResult<PersonalAccessTokenValidationResult>();

        var row = rows.Value is { Count: > 0 } list ? list[0] : null;
        if (row is null || row.IsRevoked || (row.ExpiresAt is not null && row.ExpiresAt.Value <= DateTimeOffset.UtcNow))
        {
            PersonalAccessTokenLog.ValidationFailed(Logger);
            return GenericResult<PersonalAccessTokenValidationResult>.Success(new PersonalAccessTokenValidationResult { IsValid = false });
        }

        // Why: best-effort LastUsedAt touch — a failed update must not reject an otherwise-valid token.
        _ = await NonQuery(
            "UPDATE auth.PersonalAccessToken SET LastUsedAt=sysdatetimeoffset() WHERE Id=@id",
            cancellationToken, ("@id", row.Id)).ConfigureAwait(false);

        return GenericResult<PersonalAccessTokenValidationResult>.Success(new PersonalAccessTokenValidationResult
        {
            IsValid = true,
            UserId = row.UserId,
            TokenId = row.Id,
        });
    }

    /// <inheritdoc />
    Task<IGenericResult<IReadOnlyList<PersonalAccessTokenSummary>>> IPatVault.List(Guid userId, CancellationToken cancellationToken)
        => QueryRows(
            "SELECT Id, Name, CreatedAt, ExpiresAt, LastUsedAt, IsRevoked FROM auth.PersonalAccessToken " +
            "WHERE UserId=@u AND IsRevoked=0",
            r => new PersonalAccessTokenSummary
            {
                TokenId = (Guid)r["Id"]!,
                // Why: the prefix is part of the raw token and is never stored — only label/dates display.
                Prefix = string.Empty,
                Label = (string)r["Name"]!,
                CreatedAt = ((DateTimeOffset)r["CreatedAt"]!).UtcDateTime,
                ExpiresAt = (r["ExpiresAt"] as DateTimeOffset?)?.UtcDateTime,
                LastUsedAt = (r["LastUsedAt"] as DateTimeOffset?)?.UtcDateTime,
                IsRevoked = (bool)r["IsRevoked"]!,
            },
            cancellationToken, ("@u", userId));

    /// <inheritdoc />
    public async Task<IGenericResult> Revoke(Guid userId, Guid tokenId, CancellationToken cancellationToken = default)
    {
        var res = await NonQuery(
            "UPDATE auth.PersonalAccessToken SET IsRevoked=1 WHERE Id=@id AND UserId=@u AND IsRevoked=0",
            cancellationToken, ("@id", tokenId), ("@u", userId)).ConfigureAwait(false);
        if (!res.IsSuccess)
            return res;

        if (res.Value == 0)
            return GenericResult.Failure(PersonalAccessTokenLog.TokenNotFound(Logger, userId, tokenId));

        PersonalAccessTokenLog.TokenRevoked(Logger, userId, tokenId);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public async Task<IGenericResult> RevokeAll(Guid userId, CancellationToken cancellationToken = default)
    {
        var res = await NonQuery(
            "UPDATE auth.PersonalAccessToken SET IsRevoked=1 WHERE UserId=@u AND IsRevoked=0",
            cancellationToken, ("@u", userId)).ConfigureAwait(false);
        if (!res.IsSuccess)
            return res;

        return GenericResult.Success();
    }

    // ====================================================================================
    // IAgentKeyVault — agent keys over auth.AgentKey (UserId stored as the Guid's string form)
    // ====================================================================================

    /// <inheritdoc />
    public async Task<IGenericResult<Guid>> Create(Guid userId, string userName, string rawKey, string label, DateTime? expiresAt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawKey))
            return GenericResult<Guid>.Failure(AgentKeyLog.VaultNameMissing(Logger));

        var keyId = Guid.NewGuid();
        var insert = await NonQuery(
            "INSERT auth.AgentKey (KeyId, UserId, UserName, Label, KeyHash, IsActive, ExpiresAt) " +
            "VALUES (@k, @u, @un, @l, @h, 1, @e)",
            cancellationToken,
            ("@k", keyId), ("@u", userId.ToString()), ("@un", userName), ("@l", label),
            ("@h", PepperSecret(rawKey)),
            ("@e", expiresAt.HasValue ? new DateTimeOffset(expiresAt.Value, TimeSpan.Zero) : (object?)null)).ConfigureAwait(false);
        if (!insert.IsSuccess)
            return insert.ToNewResult<Guid>();

        if (insert.Value < 1)
        {
            return GenericResult<Guid>.Failure(
                AgentKeyLog.DatabaseError(Logger, new InvalidOperationException("Agent key insert affected no rows."), "CreateKey"));
        }

        AgentKeyLog.KeyCreated(Logger, userId, keyId, label);
        return GenericResult<Guid>.Success(keyId);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Explicit implementation: <see cref="IPatVault.Validate"/> has the identical signature, and a
    /// presented secret is only meaningful against the store it was minted for.
    /// </remarks>
    async Task<IGenericResult<AgentKeyValidationResult>> IAgentKeyVault.Validate(string rawKey, CancellationToken cancellationToken)
    {
        AgentKeyLog.ValidatingKey(Logger);

        if (string.IsNullOrWhiteSpace(rawKey))
            return GenericResult<AgentKeyValidationResult>.Failure(AgentKeyLog.ValidationFailed(Logger));

        var rows = await QueryRows(
            "SELECT KeyId, UserId, IsActive, ExpiresAt FROM auth.AgentKey WHERE KeyHash=@h",
            r => new AgentKeyRow(
                (Guid)r["KeyId"]!,
                (string)r["UserId"]!,
                (bool)r["IsActive"]!,
                r["ExpiresAt"] as DateTimeOffset?),
            cancellationToken, ("@h", PepperSecret(rawKey))).ConfigureAwait(false);
        if (!rows.IsSuccess)
            return rows.ToNewResult<AgentKeyValidationResult>();

        // Why three messages and not one: an unrecognised key, a deactivated key and an expired key
        // are the same answer to the caller and three different things to whoever is looking at the
        // log. Collapsing them means an operator cannot tell a revoked agent from a typo.
        var row = rows.Value is { Count: > 0 } list ? list[0] : null;
        if (row is null)
        {
            AgentKeyLog.KeyNotRecognised(Logger);
            return GenericResult<AgentKeyValidationResult>.Success(new AgentKeyValidationResult { IsValid = false });
        }

        if (!row.IsActive)
        {
            AgentKeyLog.KeyInactive(Logger, row.KeyId);
            return GenericResult<AgentKeyValidationResult>.Success(new AgentKeyValidationResult { IsValid = false });
        }

        if (row.ExpiresAt is not null && row.ExpiresAt.Value <= DateTimeOffset.UtcNow)
        {
            AgentKeyLog.KeyExpired(Logger, row.KeyId, row.ExpiresAt.Value);
            return GenericResult<AgentKeyValidationResult>.Success(new AgentKeyValidationResult { IsValid = false });
        }

        // Why: UserId is stored as the Guid's string form. A row that will not parse back is corrupt
        // data, not a failed match — fail loud rather than reporting it as an invalid key.
        if (!Guid.TryParse(row.UserId, out var ownerId))
        {
            return GenericResult<AgentKeyValidationResult>.Failure(
                AgentKeyLog.DatabaseError(Logger, new InvalidOperationException("Agent key UserId is not a Guid."), "ValidateKey"));
        }

        // Why: best-effort LastUsedAt touch — a failed update must not reject an otherwise-valid key.
        // Why it is still logged: discarding the result silently means a column that has quietly
        // stopped updating looks identical to a key nobody has used.
        var touched = await NonQuery(
            "UPDATE auth.AgentKey SET LastUsedAt=sysdatetimeoffset() WHERE KeyId=@k",
            cancellationToken, ("@k", row.KeyId)).ConfigureAwait(false);
        if (!touched.IsSuccess)
        {
            AgentKeyLog.LastUsedNotRecorded(Logger, row.KeyId);
        }

        AgentKeyLog.KeyValidated(Logger, row.KeyId, ownerId);

        return GenericResult<AgentKeyValidationResult>.Success(new AgentKeyValidationResult
        {
            IsValid = true,
            UserId = ownerId,
            KeyId = row.KeyId,
        });
    }

    /// <inheritdoc />
    Task<IGenericResult<IReadOnlyList<AgentKeySummary>>> IAgentKeyVault.List(Guid userId, CancellationToken cancellationToken)
        => QueryRows(
            "SELECT KeyId, Label, CreateDate, ExpiresAt, LastUsedAt FROM auth.AgentKey " +
            "WHERE UserId=@u AND IsActive=1",
            r => new AgentKeySummary
            {
                KeyId = (Guid)r["KeyId"]!,
                // Why: the prefix is part of the raw key and is never stored — only label/dates display.
                Prefix = string.Empty,
                Label = (string)r["Label"]!,
                CreatedAt = ((DateTimeOffset)r["CreateDate"]!).UtcDateTime,
                ExpiresAt = (r["ExpiresAt"] as DateTimeOffset?)?.UtcDateTime,
                LastUsedAt = (r["LastUsedAt"] as DateTimeOffset?)?.UtcDateTime,
            },
            cancellationToken, ("@u", userId.ToString()));

    /// <inheritdoc />
    public async Task<IGenericResult> Delete(Guid userId, Guid keyId, CancellationToken cancellationToken = default)
    {
        var res = await NonQuery(
            "UPDATE auth.AgentKey SET IsActive=0, ModifyDate=sysdatetimeoffset() " +
            "WHERE KeyId=@k AND UserId=@u AND IsActive=1",
            cancellationToken, ("@k", keyId), ("@u", userId.ToString())).ConfigureAwait(false);
        if (!res.IsSuccess)
            return res;

        if (res.Value == 0)
            return GenericResult.Failure(AgentKeyLog.KeyNotFound(Logger, userId, keyId));

        AgentKeyLog.KeyDeleted(Logger, userId, keyId);
        return GenericResult.Success();
    }

    // Why: the stored value for a high-entropy secret (token/key) is Base64(HMAC(utf8(secret), pepper)) —
    // deterministic, so a peppered hash can be looked up by equality; the pepper never leaves the vault.
    private string PepperSecret(string rawSecret)
        => Convert.ToBase64String(Pepper(Encoding.UTF8.GetBytes(rawSecret)));

    // Why: minimal projection for PAT validation — carries only non-secret identity/status fields.
    private sealed record TokenRow(Guid Id, Guid UserId, bool IsRevoked, DateTimeOffset? ExpiresAt);

    private sealed record AgentKeyRow(Guid KeyId, string UserId, bool IsActive, DateTimeOffset? ExpiresAt);
}
