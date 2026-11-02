using System.Diagnostics.CodeAnalysis;
using Fdw.Calculations.Endpoints.CalculationEntities;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Calculations;
using Fdw.Services.Multitenancy.Abstractions;

namespace FdwReferenceCalculations.Endpoints;

/// <summary>
/// Concrete endpoint to save a version of a calculation at an organization or workspace (FDW-2560).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SaveCalculationVersionEndpoint : SaveCalculationVersionEndpointBase
{
    /// <inheritdoc />
    public SaveCalculationVersionEndpoint(
        ICalculationConfigurationProvider calculations,
        ICalculationVersionConfigurationProvider versions,
        IPlaceAncestry ancestry,
        IScopeAuthority authority,
        IAuthenticationContextAccessor authContext)
        : base(calculations, versions, ancestry, authority, authContext)
    {
    }
}
