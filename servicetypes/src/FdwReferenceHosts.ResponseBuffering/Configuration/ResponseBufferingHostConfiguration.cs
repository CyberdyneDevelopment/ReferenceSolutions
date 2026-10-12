using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Hosts.Abstractions;

namespace FdwReferenceHosts.ResponseBuffering.Configuration;

/// <summary>
/// Response buffering for this host, read from the server configuration tier.
/// </summary>
/// <remarks>
/// These settings are declared on this record rather than on IResponseBufferingImplementationConfiguration, so
/// adding an option never widens a shared interface and no other option can see settings that are
/// not its business.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "ResponseBuffering", ServiceType = "Host")]
public sealed partial class ResponseBufferingHostConfiguration : IResponseBufferingImplementationConfiguration
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


    /// <summary>Gets or sets a value indicating whether response buffering is applied.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the largest response this host will buffer, in bytes.</summary>
    public int MaxBufferBytes { get; set; }

    /// <summary>Gets the path prefixes excluded from buffering.</summary>
    public IList<ResponseBufferingExcludePath> ExcludePathPrefixes { get; set; } = new List<ResponseBufferingExcludePath>();
}
