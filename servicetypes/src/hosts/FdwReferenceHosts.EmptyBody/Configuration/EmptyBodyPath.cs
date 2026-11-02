using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace FdwReferenceHosts.EmptyBody.Configuration;

/// <summary>An exact path whose requests carry no body.</summary>
/// <remarks>Its own record with its own named columns. A set of values is a container, and what
/// each value means belongs in the schema rather than in a reader that unpacks a Name/Value pair.</remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Host", ServiceType = "EmptyBodyPath")]
public sealed partial class EmptyBodyPath : IGenericConfiguration
{
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }


    /// <summary>Gets or sets the exact path.</summary>
    public string Path { get; set; } = string.Empty;


}
