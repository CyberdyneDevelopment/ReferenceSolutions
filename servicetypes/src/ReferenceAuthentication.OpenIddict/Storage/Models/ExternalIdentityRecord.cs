using System;
using Fdw.Data;
using ReferenceAuthentication.OpenIddict;
using Fdw.Services.Authentication.OpenIddict;
using Fdw.Services.Authentication;
using Fdw.Services;
using Fdw;

namespace ReferenceAuthentication.OpenIddict.Storage.Models;

/// <summary>
/// Record model for the <c>auth.ExternalIdentity</c> table.
/// Maps an external identity (provider + external subject) to a FDW user.
/// Plain update-in-place: <c>Id</c> is the natural PK.
/// </summary>
[GenerateMapper]
internal sealed partial class ExternalIdentityRecord
{
    /// <summary>Natural primary key (NEWID on first insert).</summary>
    public Guid Id { get; set; }

    /// <summary>External identity provider name (e.g., "auth0", "azure", "google"). Case-insensitive.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Subject identifier as issued by the external provider (the <c>sub</c> claim value).</summary>
    public string ExternalSubject { get; set; } = string.Empty;

    /// <summary>FDW user this external identity maps to. Must reference a valid user in ConfigurationDb <c>users.Users</c>.</summary>
    public Guid UserId { get; set; }

    /// <summary>Whether this mapping is currently active. Inactive mappings are ignored during token issuance.</summary>
    public bool IsActive { get; set; }

    /// <summary>Row creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Last modification timestamp.</summary>
    public DateTimeOffset ModifiedAt { get; set; }
}
