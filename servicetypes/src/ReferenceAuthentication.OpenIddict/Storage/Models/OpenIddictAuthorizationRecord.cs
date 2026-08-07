using System;
using Fdw.Data;
using ReferenceAuthentication.OpenIddict;
using Fdw.Services.Authentication.OpenIddict;
using Fdw.Services.Authentication;
using Fdw.Services;
using Fdw;

namespace ReferenceAuthentication.OpenIddict.Storage.Models;

/// <summary>
/// Record model for the <c>auth.OpenIddictAuthorization</c> table.
/// Plain update-in-place: <c>Id</c> is both the natural PK and the durable identity.
/// Child scopes live in <c>auth.OpenIddictAuthorizationScope</c>; on Update they are
/// deleted and re-inserted (no version history retained on the child set).
/// </summary>
[GenerateMapper]
internal sealed partial class OpenIddictAuthorizationRecord
{
    /// <summary>Natural primary key and durable identity (NEWID on first insert).</summary>
    public Guid Id { get; set; }

    /// <summary>Logical Id of the owning application (OpenIddictApplication.Id). Null for device-code flows.</summary>
    public Guid? ApplicationId { get; set; }

    /// <summary>Subject (user) identifier. Null for client-credentials authorizations.</summary>
    public string? Subject { get; set; }

    /// <summary>Authorization status: 'valid', 'redeemed', 'revoked'.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Authorization type: 'ad-hoc' or 'permanent'.</summary>
    public string AuthorizationType { get; set; } = string.Empty;

    /// <summary>When the authorization was created by OpenIddict.</summary>
    public DateTimeOffset CreationDate { get; set; }

    /// <summary>Row creation timestamp. Set once at CreateAsync; never changed on update.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Last modification timestamp. Updated on every UpdateAsync call.</summary>
    public DateTimeOffset ModifiedAt { get; set; }
}
