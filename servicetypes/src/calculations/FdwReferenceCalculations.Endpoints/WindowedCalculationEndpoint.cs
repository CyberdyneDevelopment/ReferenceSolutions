using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Calculations.Endpoints;
using Fdw.Results;
using Fdw.Services.Calculations;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace FdwReferenceCalculations.Endpoints;

/// <summary>
/// Executes a windowed calculation and returns the result.
/// </summary>
[ExcludeFromCodeCoverage]
public class WindowedCalculationEndpoint : WindowedCalculationEndpointBase
{
    private readonly ICalculationEntityService _service;
    private readonly IDataGatewayProvider _dataGateways;

    /// <summary>
    /// Initializes a new instance of <see cref="WindowedCalculationEndpoint"/>.
    /// </summary>
    public WindowedCalculationEndpoint(
        ILogger<WindowedCalculationEndpoint> logger,
        ICalculationEntityService service,
        IDataGatewayProvider dataGateways)
        : base(logger)
    {
        _service = service;
        _dataGateways = dataGateways;
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
    protected override async Task<IGenericResult<WindowedCalculationResponse>> ExecuteWindowed(
        WindowedCalculationSpec spec,
        WindowedCalculationRequest request,
        CancellationToken cancellationToken)
    {
        var entityResult = await _service.GetCalculation(request.CalculationName, cancellationToken).ConfigureAwait(false);
        if (!entityResult.IsSuccess || entityResult.Value is not { } entity)
        {
            return entityResult.ToNewResult<WindowedCalculationResponse>();
        }

        if (entity.TypedConfiguration is not WindowedCalculationConfiguration)
        {
            return GenericResult<WindowedCalculationResponse>.Failure(
                CalculationEndpointLog.CalculationNotWindowed(EndpointLogger, entity.Name, entity.CalculationEntityType));
        }

        // The request states what runs and the most rows a data set input may hold; the calculation supplies the inputs it reads.
        var parameters = ContextParameters(spec);
        if (!parameters.IsSuccess || parameters.Value is not { } contextParameters)
        {
            return parameters.ToNewResult<WindowedCalculationResponse>();
        }

        var context = new Fdw.Calculations.CalculationContext(_dataGateways, contextParameters);
        var executed = await _service.ExecuteCalculation(entity.Name, context, cancellationToken).ConfigureAwait(false);
        if (!executed.IsSuccess || executed.Value is not { } resultJson)
        {
            return executed.ToNewResult<WindowedCalculationResponse>();
        }

        return ToResponse(entity.Name, spec, resultJson);
    }
}
