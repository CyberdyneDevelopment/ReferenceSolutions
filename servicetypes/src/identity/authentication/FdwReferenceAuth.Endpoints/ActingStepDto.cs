using System;
using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceAuth.Endpoints;

/// <summary>One session in the chain a person has built by acting with roles (FDW-2282).</summary>
[ExcludeFromCodeCoverage]
public sealed class ActingStepDto
{
    /// <summary>Gets or sets the step's number: 0 is the person's own session.</summary>
    public int Step { get; set; }

    /// <summary>Gets or sets the role the step acts with; null for the person's own session.</summary>
    public string? Role { get; set; }

    /// <summary>Gets or sets when the step started.</summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>Gets or sets a value indicating whether this is the session the request was made with.</summary>
    public bool IsCurrent { get; set; }
}
