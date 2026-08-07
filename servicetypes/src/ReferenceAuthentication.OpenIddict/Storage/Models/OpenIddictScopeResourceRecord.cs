using System;
using Fdw.Data;
using ReferenceAuthentication.OpenIddict;
using Fdw.Services.Authentication.OpenIddict;
using Fdw.Services.Authentication;
using Fdw.Services;
using Fdw;

namespace ReferenceAuthentication.OpenIddict.Storage.Models;

/// <summary>Record model for <c>auth.OpenIddictScopeResource</c>. Version-on-write via SetResourcesAsync: all current rows are superseded and a fresh set inserted.</summary>
[GenerateMapper]
internal sealed partial class OpenIddictScopeResourceRecord
{

    /// <summary>Logical Id of the parent OpenIddictScope (references auth.OpenIddictScope.Id, not RowId).</summary>
    public Guid ScopeId { get; set; }

    /// <summary>Resource URI (e.g. "https://api.fdw.local").</summary>
    public string Resource { get; set; } = string.Empty;

    /// <summary>Whether this is a current (non-superseded) resource row.</summary>
    public bool IsCurrent { get; set; }

    /// <summary>Whether this record is soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
