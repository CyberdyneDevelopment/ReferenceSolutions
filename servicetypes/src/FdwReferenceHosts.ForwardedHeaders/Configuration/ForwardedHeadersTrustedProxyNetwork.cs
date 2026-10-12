using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace FdwReferenceHosts.ForwardedHeaders.Configuration;

/// <summary>A proxy network whose forwarded headers are trusted.</summary>
/// <remarks>Its own record with its own named columns. A set of values is a container, and what
/// each value means belongs in the schema rather than in a reader that unpacks a Name/Value pair.</remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Host", ServiceType = "ForwardedHeadersTrustedProxyNetwork")]
public sealed partial class ForwardedHeadersTrustedProxyNetwork : IGenericConfiguration
{
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }


    /// <summary>Gets or sets the trusted proxy network, in CIDR form.</summary>
    public string Network { get; set; } = string.Empty;


}
