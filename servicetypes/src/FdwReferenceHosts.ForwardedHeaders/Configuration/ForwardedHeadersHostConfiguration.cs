using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Hosts.Abstractions;

namespace FdwReferenceHosts.ForwardedHeaders.Configuration;

/// <summary>
/// The proxy networks whose forwarded headers this host trusts, read from the server configuration tier.
/// </summary>
/// <remarks>
/// These settings are declared on this record rather than on IForwardedHeadersImplementationConfiguration, so
/// adding an option never widens a shared interface and no other option can see settings that are
/// not its business.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "ForwardedHeaders", ServiceType = "Host")]
public sealed partial class ForwardedHeadersHostConfiguration : IForwardedHeadersImplementationConfiguration
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


    /// <summary>Gets the trusted proxy networks in CIDR form. Empty trusts none, which is the safe default.</summary>
    public IList<ForwardedHeadersTrustedProxyNetwork> TrustedProxyNetworks { get; set; } = new List<ForwardedHeadersTrustedProxyNetwork>();
}
