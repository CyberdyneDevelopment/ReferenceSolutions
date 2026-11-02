using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace FdwReferenceHosts.Cors.Configuration;

/// <summary>A method the policy permits.</summary>
/// <remarks>Its own record with its own named columns. A set of values is a container, and what
/// each value means belongs in the schema rather than in a reader that unpacks a Name/Value pair.</remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Host", ServiceType = "CorsMethod")]
public sealed partial class CorsMethod : IGenericConfiguration
{
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }


    /// <summary>Gets or sets the permitted HTTP method.</summary>
    public string Method { get; set; } = string.Empty;


}
