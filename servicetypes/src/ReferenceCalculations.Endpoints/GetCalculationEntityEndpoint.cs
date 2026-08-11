using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Calculations.Endpoints.CalculationEntities;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;

namespace ReferenceCalculations.Endpoints;

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
    public GetCalculationEntityEndpoint(ICalculationEntityService service)
    {
        _service = service;
    }

    /// <inheritdoc/>
    protected override async Task<IGenericResult<CalculationEntityDetailDto?>> FindByIdentifier(
        CalculationEntityIdRequest request, CancellationToken ct)
    {
        var result = await _service.GetCalculationById(request.Id, ct);
        if (result.IsFailure)
        {
            return GenericResult<CalculationEntityDetailDto?>.Success((CalculationEntityDetailDto?)null);
        }

        var entity = result.Value!;
        var detail = new CalculationEntityDetailDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            CalculationEntityType = entity.CalculationEntityType,
            IsEnabled = entity.IsEnabled
        };

        return GenericResult<CalculationEntityDetailDto?>.Success((CalculationEntityDetailDto?)detail);
    }
}
