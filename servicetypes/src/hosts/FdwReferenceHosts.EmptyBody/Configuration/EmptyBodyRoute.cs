using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace FdwReferenceHosts.EmptyBody.Configuration;

/// <summary>A prefix/suffix route whose requests carry no body.</summary>
/// <remarks>Its own record with its own named columns. A set of values is a container, and what
/// each value means belongs in the schema rather than in a reader that unpacks a Name/Value pair.</remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Host", ServiceType = "EmptyBodyRoute")]
public sealed partial class EmptyBodyRoute : IGenericConfiguration
{
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }


    /// <summary>Gets or sets the path prefix the route starts with.</summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>Gets or sets the path suffix the route ends with.</summary>
    public string Suffix { get; set; } = string.Empty;


}
