using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Hosts.Abstractions;

namespace FdwReferenceHosts.OutboundIdentity.Configuration;

/// <summary>
/// The identity this host presents when it calls another service, read from the server configuration tier.
/// </summary>
/// <remarks>
/// The identity itself -- its issuer, client, secret or assertion -- is a <c>sec.Identity</c> row in platform
/// configuration. This record only says which of those rows this host authenticates as, and for which audience
/// and scopes it asks, so the same identity row can serve several hosts that each call a different audience.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "OutboundIdentity", ServiceType = "Host")]
public sealed partial class OutboundIdentityHostConfiguration : IOutboundIdentityImplementationConfiguration
{
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name this configuration is resolved by.</summary>
    // Why it maps with no column of its own: the name is the domain's, and there is one
    // name for a configured member. The domain provider joins the domain row to this one,
    // and the join's result set is what carries it in.
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the <c>sec.Identity</c> row this host authenticates as.</summary>
    public string IdentityName { get; set; } = string.Empty;

    /// <summary>Gets or sets the audience the tokens this host requests must be valid at.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Gets or sets the scopes requested, space-separated; empty when the identity derives its own.</summary>
    public string? Scopes { get; set; }
}
