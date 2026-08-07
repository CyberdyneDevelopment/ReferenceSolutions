using System;
using Fdw.Data;
using ReferenceAuthentication.OpenIddict;
using Fdw.Services.Authentication.OpenIddict;
using Fdw.Services.Authentication;
using Fdw.Services;
using Fdw;

namespace ReferenceAuthentication.OpenIddict.Storage.Models;

/// <summary>Record model for the <c>auth.OpenIddictScope</c> table. Version-on-write: Id is the durable logical identity, RowId is the version PK.</summary>
[GenerateMapper]
internal sealed partial class OpenIddictScopeRecord
{

    /// <summary>Durable logical identity shared across version rows.</summary>
    public Guid Id { get; set; }

    /// <summary>Unique scope name (e.g. "fdw.api").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Human-readable display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Whether this is the current active version.</summary>
    public bool IsCurrent { get; set; }

    /// <summary>Whether this record is soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Last modification timestamp.</summary>
    public DateTimeOffset ModifiedAt { get; set; }
}
