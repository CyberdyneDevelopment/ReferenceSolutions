using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Calculations.Endpoints;
using Fdw.Services.Calculations.Abstractions;

namespace Reference.Api.Endpoints.Calculations;

/// <summary>
/// Executes a windowed calculation and returns the result.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class WindowedCalculationEndpoint : WindowedCalculationEndpointBase
{
    private readonly ICalculationEntityService _service;

    /// <summary>
    /// Initializes a new instance of <see cref="WindowedCalculationEndpoint"/>.
    /// </summary>
    public WindowedCalculationEndpoint(ICalculationEntityService service)
    {
        _service = service;
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Summary(s =>
        {
            s.Summary = "Execute windowed calculation";
            s.Description = "Applies a window function (ROW_NUMBER, RANK, SUM, AVG, etc.) to a DataSet column with configurable partition and order fields.";
        });
        Tags("Calculations");
    }

    /// <inheritdoc/>
    protected override async Task<WindowedCalculationResponse?> ExecuteWindowed(
        WindowedCalculationSpec spec,
        WindowedCalculationRequest request,
        CancellationToken cancellationToken)
    {
        var entityResult = await _service.GetCalculation(request.CalculationName, cancellationToken).ConfigureAwait(false);
        if (entityResult.IsFailure)
        {
            return new WindowedCalculationResponse
            {
                CalculationName = request.CalculationName,
                WindowFunction = spec.WindowFunction,
                ResultField = spec.OutputFieldName,
                PartitionCount = 0,
                RowCount = 0,
                ResultJson = JsonSerializer.Serialize(new { Error = "Calculation entity not found" })
            };
        }

        var entity = entityResult.Value!;

        var context = new Fdw.Calculations.CalculationContext(
            Resolve<Fdw.Services.Data.Abstractions.IDataGateway>());

        var executeResult = await _service.ExecuteCalculation(entity.Name, context, cancellationToken).ConfigureAwait(false);
        if (executeResult.IsFailure)
        {
            return null;
        }

        return new WindowedCalculationResponse
        {
            CalculationName = entity.Name,
            WindowFunction = spec.WindowFunction,
            ResultField = spec.OutputFieldName,
            ResultJson = executeResult.Value ?? string.Empty
        };
    }
}
