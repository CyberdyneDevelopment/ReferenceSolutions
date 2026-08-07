using System;
using Fdw.Data;
using ReferenceAuthentication.OpenIddict;
using Fdw.Services.Authentication.OpenIddict;
using Fdw.Services.Authentication;
using Fdw.Services;
using Fdw;

namespace ReferenceAuthentication.OpenIddict.Storage.Models;

/// <summary>Record model for the <c>auth.OpenIddictApplicationPostLogoutRedirectUri</c> child table. Version-on-write child of <see cref="OpenIddictApplicationRecord"/>.</summary>
[GenerateMapper]
internal sealed partial class OpenIddictApplicationPostLogoutRedirectUriRecord
{

    /// <summary>Logical Id of the parent application (OpenIddictApplication.Id).</summary>
    public Guid ApplicationId { get; set; }

    /// <summary>Allowed post-logout redirect URI.</summary>
    public string Uri { get; set; } = string.Empty;

    /// <summary>Whether this is the current active version.</summary>
    public bool IsCurrent { get; set; }

    /// <summary>Whether this record is soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
