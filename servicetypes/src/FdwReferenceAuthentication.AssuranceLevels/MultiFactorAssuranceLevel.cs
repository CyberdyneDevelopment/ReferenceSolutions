using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Authentication.Abstractions.Steps;

namespace FdwReferenceAuthentication.AssuranceLevels;

/// <summary>Two or more distinct kinds of factor proved.</summary>
// Why no RestrictToCurrentCompilation: see SingleFactorAssuranceLevel.
[ExcludeFromCodeCoverage]
[TypeOption(typeof(Fdw.Services.Authentication.Abstractions.Steps.AssuranceLevels), "MultiFactor")]
public sealed class MultiFactorAssuranceLevel : AssuranceLevelBase
{
    /// <summary>Initializes a new instance of the <see cref="MultiFactorAssuranceLevel"/> class.</summary>
    public MultiFactorAssuranceLevel()
        : base(
            name: "MultiFactor",
            acrValue: "urn:cyberdyne:acr:multi-factor",
            factorKinds: 2,
            description: "Two or more distinct kinds of factor were proved.")
    {
    }
}
