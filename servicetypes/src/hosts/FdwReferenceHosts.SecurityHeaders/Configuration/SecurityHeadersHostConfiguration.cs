using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Hosts.Abstractions;

namespace FdwReferenceHosts.SecurityHeaders.Configuration;

/// <summary>
/// The security headers this host writes, read from the server configuration tier.
/// </summary>
/// <remarks>
/// These settings are declared on this record rather than on ISecurityHeadersImplementationConfiguration, so
/// adding an option never widens a shared interface and no other option can see settings that are
/// not its business.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "SecurityHeaders", ServiceType = "Host")]
public sealed partial class SecurityHeadersHostConfiguration : ISecurityHeadersImplementationConfiguration
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


    /// <summary>Gets or sets a value indicating whether the response may be framed.</summary>
    public bool AllowFraming { get; set; }

    /// <summary>Gets or sets a value indicating whether the default content-security policy is written.</summary>
    public bool EnableDefaultCsp { get; set; }
}
