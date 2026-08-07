using System;
using Fdw.Data;
using ReferenceAuthentication.OpenIddict;
using Fdw.Services.Authentication.OpenIddict;
using Fdw.Services.Authentication;
using Fdw.Services;
using Fdw;

namespace ReferenceAuthentication.OpenIddict.Storage.Models;

/// <summary>
/// Record model for the <c>auth.OpenIddictAuthorizationScope</c> child table.
/// Plain delete-then-insert: on every UpdateAsync the entire set for a given
/// AuthorizationId is deleted and the new set is inserted. No version history.
/// </summary>
[GenerateMapper]
internal sealed partial class OpenIddictAuthorizationScopeRecord
{

    /// <summary>Logical Id of the parent authorization (OpenIddictAuthorization.Id).</summary>
    public Guid AuthorizationId { get; set; }

    /// <summary>Scope name string (e.g. 'fdw.api', 'openid', 'offline_access').</summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>Creation timestamp. Set once at insert; the row is never updated.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
