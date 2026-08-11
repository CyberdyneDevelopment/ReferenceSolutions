using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Calculations.Endpoints.CalculationEntities;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;

namespace ReferenceCalculations.Endpoints;

/// <summary>
/// Soft-deletes a calculation entity.
/// </summary>
[ExcludeFromCodeCoverage]
public class DeleteCalculationEntityEndpoint : DeleteCalculationEntityEndpointBase
{
    private readonly ICalculationEntityService _service;

    /// <summary>
    /// Initializes a new instance of <see cref="DeleteCalculationEntityEndpoint"/>.
    /// </summary>
    public DeleteCalculationEntityEndpoint(ICalculationEntityService service)
    {
        _service = service;
    }

    /// <inheritdoc/>
    protected override async Task<IGenericResult<bool>> CheckExistsForDelete(
        CalculationEntityIdRequest request, CancellationToken ct)
    {
        var result = await _service.GetCalculationById(request.Id, ct).ConfigureAwait(false);
        return GenericResult<bool>.Success(result.IsSuccess);
    }

    /// <inheritdoc/>
    protected override async Task<IGenericResult> Delete(
        CalculationEntityIdRequest request, CancellationToken ct)
    {
        return await _service.DeleteCalculation(request.Id, ct).ConfigureAwait(false);
    }
}
