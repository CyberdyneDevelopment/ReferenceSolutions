using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace FdwReferenceMultitenancy.Sql.Models;

/// <summary>
/// The columns written for the implementation row of a new organization (FDW-1758): only the key to the domain row.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class SqlOrganizationConfigurationInsert
{
    /// <summary>Gets or sets the key of the organization's domain row.</summary>
    public int OrganizationRowId { get; set; }

    /// <summary>Gets or sets the tenant's identifier.</summary>
    public Guid TenantId { get; set; }
}
