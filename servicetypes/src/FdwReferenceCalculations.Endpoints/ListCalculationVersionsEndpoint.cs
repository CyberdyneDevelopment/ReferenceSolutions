using System.Diagnostics.CodeAnalysis;
using Fdw.Calculations.Endpoints.CalculationEntities;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Calculations;
using Fdw.Services.Multitenancy.Abstractions;

namespace FdwReferenceCalculations.Endpoints;

/// <summary>
/// Concrete endpoint to list the versions of a calculation at the places the caller may read (FDW-2560).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListCalculationVersionsEndpoint : ListCalculationVersionsEndpointBase
{
    /// <inheritdoc />
    public ListCalculationVersionsEndpoint(
        ICalculationConfigurationProvider calculations,
        ICalculationVersionConfigurationProvider versions,
        IPlaceAncestry ancestry,
        IScopeAuthority authority,
        IAuthenticationContextAccessor authContext,
        IPlaceNames names)
        : base(calculations, versions, ancestry, authority, authContext, names)
    {
    }
}
