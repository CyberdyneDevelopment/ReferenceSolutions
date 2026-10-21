using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceEtl.Endpoints.Nodes;

/// <summary>A prerequisite between two pipelines a step holds: <see cref="Pipeline"/> starts only after <see cref="Needs"/> has succeeded.</summary>
[ExcludeFromCodeCoverage]
public sealed class NodePrerequisiteRequest
{
    /// <summary>Gets or sets the name of the pipeline that has the prerequisite.</summary>
    public string Pipeline { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the pipeline that must succeed first.</summary>
    public string Needs { get; set; } = string.Empty;
}
