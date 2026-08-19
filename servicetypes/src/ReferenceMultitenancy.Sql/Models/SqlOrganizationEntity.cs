using System;
using Fdw.Data;
using Fdw.Services.Multitenancy.Abstractions;

namespace ReferenceMultitenancy.Sql.Models;

/// <summary>
/// Database entity for organization records (<c>tenant.Organizations</c>).
/// Has the <c>[GenerateMapper]</c> attribute so the DataGateway can hydrate rows.
/// Mapped to the abstract <see cref="OrganizationConfiguration"/> by
/// <see cref="ToConfiguration"/>.
/// </summary>
[GenerateMapper]
public sealed partial class SqlOrganizationEntity
{

    /// <summary>Gets or sets the logical org identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the logical tenant identifier.</summary>
    public Guid TenantId { get; set; }


    /// <summary>Gets or sets the display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the URL slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this is the default org for its tenant.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Gets or sets whether this is the admin org for its tenant.</summary>
    public bool IsAdminOrg { get; set; }

    /// <summary>Gets or sets whether this org is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the optional visibility group identifier.</summary>
    public Guid? VisibilityGroupId { get; set; }

    /// <summary>Gets or sets whether this is the current row.</summary>
    public bool IsCurrent { get; set; }

    /// <summary>Gets or sets whether this row is soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the source create date.</summary>
    public DateTimeOffset SrcCreateDate { get; set; }

    /// <summary>Gets or sets the application create date.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the create-by user.</summary>
    public string? CreateBy { get; set; }

    /// <summary>Gets or sets the on-behalf-of user for create.</summary>
    public string? CreateOnBehalfOf { get; set; }

    /// <summary>Gets or sets the last modification date.</summary>
    public DateTimeOffset? ModifyDate { get; set; }

    /// <summary>Gets or sets the modify-by user.</summary>
    public string? ModifyBy { get; set; }

    /// <summary>Gets or sets the on-behalf-of user for modify.</summary>
    public string? ModifyOnBehalfOf { get; set; }

    /// <summary>Maps this entity to the abstract <see cref="OrganizationConfiguration"/>.</summary>
    public OrganizationConfiguration ToConfiguration() => new()
    {
        Id = Id,
        TenantId = TenantId,
        Name = Name,
        Slug = Slug,
        IsDefault = IsDefault,
        IsAdminOrg = IsAdminOrg,
        IsActive = IsActive,
        VisibilityGroupId = VisibilityGroupId,
        IsCurrent = IsCurrent,
        IsDeleted = IsDeleted,
        SrcCreateDate = SrcCreateDate,
        CreateDate = CreateDate,
        CreateBy = CreateBy,
        CreateOnBehalfOf = CreateOnBehalfOf,
        ModifyDate = ModifyDate,
        ModifyBy = ModifyBy,
        ModifyOnBehalfOf = ModifyOnBehalfOf
    };
}
