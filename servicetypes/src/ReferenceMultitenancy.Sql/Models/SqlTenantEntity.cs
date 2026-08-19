using System;
using Fdw.Data;

namespace ReferenceMultitenancy.Sql.Models;

/// <summary>
/// Database entity for tenant records.
/// </summary>
[GenerateMapper]
public sealed partial class SqlTenantEntity
{
    /// <summary>
    /// Gets or sets the tenant unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant slug (URL-friendly identifier).
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the organization prefix applied to permission policy names on the
    /// API surface (e.g. "acme" → "acme:connections:read"). Null/empty means no prefix.
    /// </summary>
    public string? OrgPrefix { get; set; }

    /// <summary>
    /// Gets or sets the tenant display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the tenant is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the connection name for tenant-specific data.
    /// </summary>
    public string? ConnectionName { get; set; }

    /// <summary>
    /// Gets or sets the primary theme color.
    /// </summary>
    public string? PrimaryColor { get; set; }

    /// <summary>
    /// Gets or sets the secondary theme color.
    /// </summary>
    public string? SecondaryColor { get; set; }

    /// <summary>
    /// Gets or sets the accent theme color.
    /// </summary>
    public string? AccentColor { get; set; }

    /// <summary>
    /// Gets or sets the background surface color.
    /// </summary>
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the surface color.
    /// </summary>
    public string? SurfaceColor { get; set; }

    /// <summary>
    /// Gets or sets the overlay surface color.
    /// </summary>
    public string? OverlayColor { get; set; }

    /// <summary>
    /// Gets or sets the success feedback color.
    /// </summary>
    public string? SuccessColor { get; set; }

    /// <summary>
    /// Gets or sets the warning feedback color.
    /// </summary>
    public string? WarningColor { get; set; }

    /// <summary>
    /// Gets or sets the error feedback color.
    /// </summary>
    public string? ErrorColor { get; set; }

    /// <summary>
    /// Gets or sets the info feedback color.
    /// </summary>
    public string? InfoColor { get; set; }

    /// <summary>
    /// Gets or sets the main typography color.
    /// </summary>
    public string? TextMainColor { get; set; }

    /// <summary>
    /// Gets or sets the muted typography color.
    /// </summary>
    public string? TextMutedColor { get; set; }

    /// <summary>
    /// Gets or sets the logo URL.
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Gets or sets the favicon URL.
    /// </summary>
    public string? FaviconUrl { get; set; }

    /// <summary>
    /// Gets or sets the custom CSS URL.
    /// </summary>
    public string? CustomCssUrl { get; set; }

    /// <summary>
    /// Gets or sets whether dark mode is the default.
    /// </summary>
    public bool DarkModeDefault { get; set; }

    /// <summary>
    /// Gets or sets whether this tenant is the global/home tenant.
    /// Mapped from <c>tenant.Tenants.IsGlobal</c>. Exactly one current tenant has this flag set.
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of users allowed.
    /// </summary>
    public int? MaxUsers { get; set; }

    /// <summary>
    /// Gets or sets the storage quota in bytes.
    /// </summary>
    public long? StorageQuotaBytes { get; set; }

    /// <summary>
    /// Gets or sets the API rate limit per minute.
    /// </summary>
    public int? ApiRateLimitPerMinute { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last modification timestamp.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; set; }
}
