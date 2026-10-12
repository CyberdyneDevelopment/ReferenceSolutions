using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Hosts.Abstractions;

namespace FdwReferenceHosts.EmptyBody.Configuration;

/// <summary>
/// The routes this host accepts with no request body, read from the server configuration tier.
/// </summary>
/// <remarks>
/// These settings are declared on this record rather than on IEmptyBodyImplementationConfiguration, so
/// adding an option never widens a shared interface and no other option can see settings that are
/// not its business.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "EmptyBody", ServiceType = "Host")]
public sealed partial class EmptyBodyHostConfiguration : IEmptyBodyImplementationConfiguration
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


    /// <summary>Gets the exact paths accepted with no request body.</summary>
    public IList<EmptyBodyPath> BodylessPaths { get; set; } = new List<EmptyBodyPath>();

    /// <summary>Gets the prefix/suffix route pairs accepted with no request body, each \"prefix|suffix\".</summary>
    public IList<EmptyBodyRoute> BodylessRoutes { get; set; } = new List<EmptyBodyRoute>();
}
