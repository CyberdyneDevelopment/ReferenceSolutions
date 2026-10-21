using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceEtl.Endpoints.Nodes;

/// <summary>A pipeline a step holds, named as the other endpoints name it, and where it sits among the step's pipelines.</summary>
[ExcludeFromCodeCoverage]
public sealed class NodePipelineRequest
{
    /// <summary>Gets or sets the name of the pipeline.</summary>
    public string Pipeline { get; set; } = string.Empty;

    /// <summary>Gets or sets the pipeline's place among the step's pipelines; pipelines with no prerequisite between them start in this order.</summary>
    public int Ordinal { get; set; }
}
