using System;
using Fdw.Services.SecretManagers.Sqlite.Commands;
using Fdw.Services.SecretManagers.Sqlite.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.Sqlite.Handlers;

/// <summary>
/// Row model for the secrets table read via SqliteDataReader.
/// </summary>
public sealed record SecretRow(
    string SecretKey,
    string SecretValue,
    int Version,
    string SecretType,
    string? Description,
    DateTime? ExpiresAt,
    DateTime CreateDate,
    DateTime ModifyDate,
    bool IsCurrent,
    bool IsDeleted);
