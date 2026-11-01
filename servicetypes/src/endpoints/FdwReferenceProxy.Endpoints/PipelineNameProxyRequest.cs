using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Names the pipeline a proxied read is for.</summary>
[ExcludeFromCodeCoverage]
public sealed class PipelineNameProxyRequest
{
    /// <summary>Gets or sets the pipeline's name, taken from the body of a read and from the route of the other proxied calls.</summary>
    public string Name { get; set; } = string.Empty;
}
