using System.Diagnostics.CodeAnalysis;
using Fdw.Calculations.Endpoints.CalculationEntities;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Calculations;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging;

namespace FdwReferenceCalculations.Endpoints;

/// <summary>
/// Lists calculation entities, one page at a time.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListCalculationEntitiesEndpoint : ListCalculationEntitiesEndpointBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="ListCalculationEntitiesEndpoint"/>.
    /// </summary>
    public ListCalculationEntitiesEndpoint(
        ILogger<ListCalculationEntitiesEndpoint> logger,
        ICalculationConfigurationProvider provider,
        ICalculationVersionConfigurationProvider versions,
        IPlaceAncestry ancestry,
        IScopeAuthority authority,
        IAuthenticationContextAccessor authContext)
        : base(logger, provider, versions, ancestry, authority, authContext)
    {
    }
}
