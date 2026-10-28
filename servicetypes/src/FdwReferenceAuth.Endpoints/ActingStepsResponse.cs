using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceAuth.Endpoints;

/// <summary>The chain of sessions a person has stepped through by acting with roles (FDW-2282).</summary>
[ExcludeFromCodeCoverage]
public sealed class ActingStepsResponse
{
    /// <summary>Gets or sets the steps in order, the person's own session first.</summary>
    public IReadOnlyList<ActingStepDto> Steps { get; set; } = [];
}
