using System;

namespace FdwReferenceSecretManagers.Sqlite.Handlers;

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
