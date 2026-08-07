using System;
using Fdw.Data;
using ReferenceAuthentication.OpenIddict;
using Fdw.Services.Authentication.OpenIddict;
using Fdw.Services.Authentication;
using Fdw.Services;
using Fdw;

namespace ReferenceAuthentication.OpenIddict.Storage.Models;

/// <summary>Record model for the <c>auth.OpenIddictApplication</c> table. Version-on-write: Id is the durable logical identity, RowId is the version PK.</summary>
[GenerateMapper]
internal sealed partial class OpenIddictApplicationRecord
{

    /// <summary>Durable logical identity shared across version rows.</summary>
    public Guid Id { get; set; }

    /// <summary>OAuth2 client_id. Unique among active applications.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>OpenIddict client type: 'public' or 'confidential'.</summary>
    public string ClientType { get; set; } = string.Empty;

    /// <summary>OpenIddict consent type. Null for public clients that don't require consent.</summary>
    public string? ConsentType { get; set; }

    /// <summary>Human-readable display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>OpenIddict application type (e.g. 'web', 'native').</summary>
    public string? ApplicationType { get; set; }

    /// <summary>Bcrypt hash of the client secret. Null for public clients.</summary>
    public string? ClientSecretHash { get; set; }

    /// <summary>Whether this is the current active version.</summary>
    public bool IsCurrent { get; set; }

    /// <summary>Whether this record is soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Last modification timestamp.</summary>
    public DateTimeOffset ModifiedAt { get; set; }
}
