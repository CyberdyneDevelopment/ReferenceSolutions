using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;
using Fdw.Services.SecretManagers.MsSql.Commands;
using Fdw.Services.SecretManagers.MsSql.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.MsSql.Handlers;

/// <summary>
/// Row model for the auth.PersonalAccessToken table, materialized by the generated POCO mapper.
/// </summary>
/// <remarks>
/// <para>
/// Why: property names were kept unchanged from the pre-refactor shape per the gateway-migration
/// task spec. The real auth.PersonalAccessToken DDL column is "Name" (not "Label") and "CreatedAt"
/// (not "CreateDate") — the generated mapper matches column names to property names by default, so
/// these two properties will fail at runtime ("Invalid column name") until a specialist either
/// renames them or adds [Column("Name")]/[Column("CreatedAt")]. Flagged, not silently patched.
/// </para>
/// <para>
/// Why Label is <c>string?</c>, not <c>required string</c>, despite the DDL's Name column being
/// NOT NULL: <c>StoreCredentialCommand.Label</c> is a legitimately optional caller input. Forcing it
/// to a required non-null column would require a <c>??</c> fallback (e.g. <c>?? string.Empty</c>) at
/// the call site, which the NO-FALLBACKS rule forbids for real business data (as opposed to a
/// deliberately-unused placeholder field in a WHERE-scoped UPDATE). Reconciling the optional-Label
/// contract with the NOT NULL column is a business-rule decision left to a specialist.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class PersonalAccessTokenRow
{
    /// <summary>Gets or sets the token's identity (column Id).</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the owning user's identifier (column UserId).</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the hashed token value (column TokenHash).</summary>
    public required string TokenHash { get; set; }

    /// <summary>Gets or sets the optional caller-supplied label (see remarks re: real DDL column mismatch).</summary>
    public string? Label { get; set; }

    /// <summary>Gets or sets the optional expiration timestamp (column ExpiresAt, nullable).</summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>Gets or sets whether the token has been revoked (column IsRevoked).</summary>
    public bool IsRevoked { get; set; }

    /// <summary>Gets or sets the creation timestamp (see remarks re: real DDL column mismatch).</summary>
    public DateTimeOffset CreateDate { get; set; }
}
