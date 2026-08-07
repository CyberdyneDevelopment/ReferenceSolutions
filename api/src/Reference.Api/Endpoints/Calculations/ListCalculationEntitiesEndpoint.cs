using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Calculations.Endpoints.CalculationEntities;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;

namespace Reference.Api.Endpoints.Calculations;

/// <summary>
/// Lists all calculation entities.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListCalculationEntitiesEndpoint : ListCalculationEntitiesEndpointBase
{
    private readonly ICalculationEntityService _service;

    /// <summary>
    /// Initializes a new instance of <see cref="ListCalculationEntitiesEndpoint"/>.
    /// </summary>
    public ListCalculationEntitiesEndpoint(ICalculationEntityService service)
    {
        _service = service;
    }

    /// <inheritdoc/>
    protected override async Task<IGenericResult<List<CalculationEntitySummaryDto>>> LoadItems(CancellationToken ct)
    {
        var result = await _service.ListCalculations(ct);
        if (result.IsFailure)
        {
            return result.ToNewResult<List<CalculationEntitySummaryDto>>();
        }

        var summaries = (result.Value ?? [])
            .Select(e => new CalculationEntitySummaryDto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                CalculationEntityType = e.CalculationEntityType,
                IsEnabled = e.IsEnabled
            })
            .ToList();

        return GenericResult<List<CalculationEntitySummaryDto>>.Success(summaries);
    }
}
