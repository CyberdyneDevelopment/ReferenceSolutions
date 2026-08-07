using System.Diagnostics.CodeAnalysis;
using Fdw.Data;
using Fdw.Services.SecretManagers.MsSql.Commands;
using Fdw.Services.SecretManagers.MsSql.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.MsSql.Handlers;

/// <summary>
/// Row model for reading the password hash from the usr.Users table, materialized by the
/// generated POCO mapper.
/// </summary>
/// <remarks>
/// Why: usr.Users (ConfigurationDb) currently has NO PasswordHash column — credential storage moved
/// to the auth.UserSecret vault (AuthDb) with separate Salt/AlgorithmName tracking. This row's
/// property names were kept unchanged from the pre-refactor shape per the gateway-migration task
/// spec; the PasswordHash lookup will fail at runtime ("Invalid column name") until a specialist
/// repoints MsSqlStoreCredentialHandler/MsSqlVerifyCredentialHandler at the vault. Flagged, not
/// silently patched — see NO FALLBACKS / DIAGNOSE AT SYSTEM LEVEL.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class UserPasswordRow
{
    /// <summary>Gets or sets the password hash (column PasswordHash).</summary>
    public required string PasswordHash { get; set; }

    /// <summary>Gets or sets the username (column Username).</summary>
    public required string Username { get; set; }
}
