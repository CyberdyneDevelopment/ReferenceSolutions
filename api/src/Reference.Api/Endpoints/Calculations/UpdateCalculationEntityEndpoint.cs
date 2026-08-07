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
/// Updates an existing calculation entity.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateCalculationEntityEndpoint : UpdateCalculationEntityEndpointBase
{
    private readonly ICalculationEntityService _service;

    /// <summary>
    /// Initializes a new instance of <see cref="UpdateCalculationEntityEndpoint"/>.
    /// </summary>
    public UpdateCalculationEntityEndpoint(ICalculationEntityService service)
    {
        _service = service;
    }

    /// <inheritdoc/>
    protected override async Task<IGenericResult<CalculationEntityDetailDto?>> FindForUpdate(
        UpdateCalculationEntityRequest request, CancellationToken ct)
    {
        var result = await _service.GetCalculationById(request.Id, ct).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return GenericResult<CalculationEntityDetailDto?>.Success((CalculationEntityDetailDto?)null);
        }

        var entity = result.Value!;
        var detail = MapToDetail(entity);

        return GenericResult<CalculationEntityDetailDto?>.Success((CalculationEntityDetailDto?)detail);
    }

    /// <inheritdoc/>
    protected override async Task<IGenericResult<CalculationEntityDetailDto>> Update(
        UpdateCalculationEntityRequest request,
        CalculationEntityDetailDto existing,
        CancellationToken ct)
    {
        var inputs = MapInputs(request.Inputs);
        var output = new CalculationOutputSpec
        {
            OutputDataSetName = request.OutputDataSetName ?? string.Empty,
            ResultFieldName = request.ResultFieldName ?? string.Empty,
            ResultDataTypeName = request.ResultDataTypeName
        };

        // Why: header-only update; the typed body (formula) is persisted via the designer
        // Compile path, so no typedConfiguration is supplied here.
        var result = await _service.UpdateCalculation(
            request.Id,
            request.Name,
            request.Description,
            request.CalculationEntityType,
            inputs,
            output,
            request.IsEnabled,
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
