using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Calculations.Endpoints.CalculationEntities;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;

namespace ReferenceCalculations.Endpoints;

/// <summary>
/// Creates a new calculation entity.
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateCalculationEntityEndpoint : CreateCalculationEntityEndpointBase
{
    private readonly ICalculationEntityService _service;

    /// <summary>
    /// Initializes a new instance of <see cref="CreateCalculationEntityEndpoint"/>.
    /// </summary>
    public CreateCalculationEntityEndpoint(ICalculationEntityService service)
    {
        _service = service;
    }

    /// <inheritdoc/>
    protected override async Task<IGenericResult<bool>> CheckExists(
        CreateCalculationEntityRequest request, CancellationToken ct)
    {
        var result = await _service.GetCalculation(request.Name, ct).ConfigureAwait(false);
        return GenericResult<bool>.Success(result.IsSuccess);
    }

    /// <inheritdoc/>
    protected override async Task<IGenericResult<CalculationEntityDetailDto>> Create(
        CreateCalculationEntityRequest request, CancellationToken ct)
    {
        var inputs = MapInputs(request.Inputs);
        var output = new CalculationOutputSpec
        {
            OutputDataSetName = request.OutputDataSetName ?? string.Empty,
            ResultFieldName = request.ResultFieldName ?? string.Empty,
            ResultDataTypeName = request.ResultDataTypeName
        };

        // Why: this endpoint creates the calculation header only; the typed body (formula)
        // is set via the designer Compile path, so no typedConfiguration is supplied here.
        var result = await _service.CreateCalculation(
            request.Name,
            request.Description,
            request.CalculationEntityType,
            inputs,
            output,
            cancellationToken: ct).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return result.ToNewResult<CalculationEntityDetailDto>();
        }

        return GenericResult<CalculationEntityDetailDto>.Success(
            MapToDetail(result.Value!));
    }

    private static List<CalculationInput> MapInputs(IList<CalculationEntityInputDto> dtos)
    {
        return dtos.Select(dto => new CalculationInput
        {
            Kind = CalculationInputKinds.ByName(dto.InputKind),
            DataSetName = dto.DataSetName,
            ConnectionName = dto.ConnectionName,
            ContainerPath = dto.ContainerPath,
            InputAlias = dto.InputAlias
        }).ToList();
    }

    private static CalculationEntityDetailDto MapToDetail(ICalculationEntity entity)
    {
        return new CalculationEntityDetailDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            CalculationEntityType = entity.CalculationEntityType,
            OutputDataSetName = entity.Output.OutputDataSetName,
            ResultFieldName = entity.Output.ResultFieldName,
            ResultDataTypeName = entity.Output.ResultDataTypeName,
            IsEnabled = entity.IsEnabled,
            Inputs = entity.Inputs.Select(i => new CalculationEntityInputDto
            {
                InputAlias = i.InputAlias,
                InputKind = i.Kind?.Name ?? "DataSet",
                DataSetName = i.DataSetName,
                ConnectionName = i.ConnectionName,
                ContainerPath = i.ContainerPath,
                ScalarValueType = i.ScalarValue?.ValueType?.Name,
                ScalarSerializedValue = i.ScalarValue?.SerializedValue
            }).ToList()
        };
    }
}
