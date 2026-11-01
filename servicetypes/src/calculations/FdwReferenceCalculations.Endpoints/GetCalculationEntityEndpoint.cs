using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Calculations.Endpoints.CalculationEntities;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;
using Microsoft.Extensions.Logging;

namespace FdwReferenceCalculations.Endpoints;

/// <summary>
/// Gets a calculation entity by ID.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetCalculationEntityEndpoint : GetCalculationEntityEndpointBase
{
    private readonly ICalculationEntityService _service;

    /// <summary>
    /// Initializes a new instance of <see cref="GetCalculationEntityEndpoint"/>.
    /// </summary>
    public GetCalculationEntityEndpoint(ILogger<GetCalculationEntityEndpoint> logger, ICalculationEntityService service) : base(logger)
    {
        _service = service;
    }

    /// <inheritdoc/>
    protected override async Task<IGenericResult<CalculationEntityDetailDto?>> FindByIdentifier(
        CalculationEntityIdRequest request, CancellationToken ct)
    {
        return CalculationEntityDetailMapper.Found(await _service.GetCalculationById(request.Id, ct).ConfigureAwait(false));
    }
}
