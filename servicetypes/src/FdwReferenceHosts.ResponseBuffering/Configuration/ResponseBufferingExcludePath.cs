using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace FdwReferenceHosts.ResponseBuffering.Configuration;

/// <summary>A path prefix that bypasses buffering.</summary>
/// <remarks>Its own record with its own named columns. A set of values is a container, and what
/// each value means belongs in the schema rather than in a reader that unpacks a Name/Value pair.</remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Host", ServiceType = "ResponseBufferingExcludePath")]
public sealed partial class ResponseBufferingExcludePath : IGenericConfiguration
{
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }


    /// <summary>Gets or sets the path prefix that bypasses buffering.</summary>
    public string PathPrefix { get; set; } = string.Empty;


}
