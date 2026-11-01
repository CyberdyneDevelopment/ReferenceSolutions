using System.Diagnostics.CodeAnalysis;
using Fdw.Calculations.Endpoints.CalculationEntities;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Calculations;
using Fdw.Services.Multitenancy.Abstractions;

namespace FdwReferenceCalculations.Endpoints;

/// <summary>
/// Concrete endpoint to delete a version of a calculation from an organization or workspace (FDW-2560).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteCalculationVersionEndpoint : DeleteCalculationVersionEndpointBase
{
    /// <inheritdoc />
    public DeleteCalculationVersionEndpoint(
        ICalculationConfigurationProvider calculations,
        ICalculationVersionConfigurationProvider versions,
        IPlaceAncestry ancestry,
        IScopeAuthority authority,
        IAuthenticationContextAccessor authContext)
        : base(calculations, versions, ancestry, authority, authContext)
    {
    }
}
