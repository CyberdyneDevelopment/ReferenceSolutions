using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Calculations.Endpoints;
using Fdw.Services.Calculations.Abstractions.Caching;
using Fdw.Web.Calculations.Clients.Models;

namespace Reference.Api.Endpoints.Calculations;

/// <summary>
/// Closure for the execute calculation endpoint with caching support.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ExecuteCalculationEndpoint : ExecuteCalculationEndpointBase
{
    private readonly ICalculationCacheService _cacheService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecuteCalculationEndpoint"/> class.
    /// </summary>
    public ExecuteCalculationEndpoint(ICalculationCacheService cacheService)
    {
        _cacheService = cacheService;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("datasets:write");
        Summary(s =>
        {
            s.Summary = "Execute a calculation";
            s.Description = "Executes the specified calculation type on the provided array of values. Results are cached for performance.";
        });
        Tags("Calculations");
    }

    /// <inheritdoc />
    protected override async Task OnCalculationExecuted(ExecuteCalculationRequest req, ExecuteCalculationResponse response, CancellationToken ct)
    {
        var cacheResult = await _cacheService.Set(req.CalculationType, req.Values.ToArray(), response.Result, null, ct).ConfigureAwait(false);
        if (!cacheResult.IsSuccess)
        {
            CalculationEndpointLog.ValidationFailed(EndpointLogger, cacheResult.CurrentMessage ?? "Cache write failed");
        }
    }
}
