using System.Diagnostics.CodeAnalysis;

namespace SampleStep;

/// <summary>
/// What this step needs to know to do its job.
/// </summary>
/// <remarks>
/// One instance per configured use. Two providers of the same kind are this record twice, not two
/// steps — which is why adding a second identity provider is a configuration row rather than a
/// package.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed record SampleStepConfiguration
{
    /// <summary>Gets an example setting.</summary>
    /// <remarks>Required with no default: a value the caller did not choose is a value nobody chose.</remarks>
    public required string Example { get; init; }
}
