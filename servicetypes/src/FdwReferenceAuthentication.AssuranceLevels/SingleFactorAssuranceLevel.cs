using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Authentication.Abstractions.Steps;

namespace FdwReferenceAuthentication.AssuranceLevels;

/// <summary>One kind of factor proved: something known, something held or something inherent.</summary>
// Why no RestrictToCurrentCompilation: on a member that flag withholds it from consuming assemblies,
// and this level does not live in the assembly that declares the collection. Left set, it never joins
// AssuranceLevels and no token would carry it.
[ExcludeFromCodeCoverage]
[TypeOption(typeof(Fdw.Services.Authentication.Abstractions.Steps.AssuranceLevels), "SingleFactor")]
public sealed class SingleFactorAssuranceLevel : AssuranceLevelBase
{
    /// <summary>Initializes a new instance of the <see cref="SingleFactorAssuranceLevel"/> class.</summary>
    public SingleFactorAssuranceLevel()
        : base(
            name: "SingleFactor",
            acrValue: "urn:cyberdyne:acr:single-factor",
            factorKinds: 1,
            description: "One kind of factor was proved.")
    {
    }
}
