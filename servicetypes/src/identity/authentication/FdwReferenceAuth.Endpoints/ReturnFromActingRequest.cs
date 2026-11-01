using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceAuth.Endpoints;

/// <summary>The step to return to when a person stops acting with a role (FDW-2282).</summary>
[ExcludeFromCodeCoverage]
public sealed class ReturnFromActingRequest
{
    /// <summary>
    /// Gets or sets the step to return to: 0 is the person's own session, 1 the first role they acted with, and so on. Each step is one
    /// the person is shown in the chain of steps they have taken.
    /// </summary>
    public int Step { get; set; }
}
