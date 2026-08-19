using System;
using Fdw.Data;

namespace ReferenceMultitenancy.Sql.Models;

/// <summary>
/// Database entity for tenant feature flags.
/// </summary>
[GenerateMapper]
public sealed partial class TenantFeatureEntity
{
    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the feature name.
    /// </summary>
    public string Feature { get; set; } = string.Empty;
}
