using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;
using Fdw.Services.SecretManagers.MsSql.Commands;
using Fdw.Services.SecretManagers.MsSql.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.MsSql.Handlers;

/// <summary>Row model for the sec.Secret table, materialized by the generated POCO mapper.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class SecretRow
{
    /// <summary>Gets or sets the secret's logical key (column SecretKey).</summary>
    public required string SecretKey { get; set; }

    /// <summary>Gets or sets the secret's plaintext value as stored (column SecretValue).</summary>
    public required string SecretValue { get; set; }

    /// <summary>Gets or sets the version number (column Version).</summary>
    public int Version { get; set; }

    /// <summary>Gets or sets the secret type discriminator (column SecretType).</summary>
    public required string SecretType { get; set; }

    /// <summary>Gets or sets the optional description (column Description, nullable).</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the optional expiration timestamp (column ExpiresAt, nullable).</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Gets or sets the row creation timestamp (column CreateDate).</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the row's last modification timestamp (column ModifyDate).</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets whether this row is the current version (column IsCurrent).</summary>
    public bool IsCurrent { get; set; }

    /// <summary>Gets or sets whether this row has been soft-deleted (column IsDeleted).</summary>
    public bool IsDeleted { get; set; }
}
