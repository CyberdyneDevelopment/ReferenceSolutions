using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Calculations.Endpoints.CalculationEntities;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using Fdw.Calculations.Endpoints;
using Fdw.Configuration;

namespace FdwReferenceCalculations.Endpoints;

/// <summary>
/// Updates an existing calculation entity.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateCalculationEntityEndpoint : UpdateCalculationEntityEndpointBase
{
    private readonly ICalculationEntityService _service;

    /// <summary>
    /// Initializes a new instance of <see cref="UpdateCalculationEntityEndpoint"/>.
    /// </summary>
    public UpdateCalculationEntityEndpoint(ILogger<UpdateCalculationEntityEndpoint> logger, ICalculationEntityService service) : base(logger)
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
        // Every field merges from the existing row -- an omitted (null) request property leaves it
        // unchanged, matching UpdateConnectionEndpointBase's pattern. Inputs is the one collection:
        // null means "not sent, keep the existing set"; an empty list is a real request to clear it,
        // which is why the request property has no default and this checks null, not Count == 0.
        var inputs = request.Inputs is not null ? MapInputs(request.Inputs) : MapInputs(existing.Inputs);
        var output = new CalculationOutputSpec
        {
            OutputDataSetName = request.OutputDataSetName ?? existing.OutputDataSetName ?? string.Empty,
            ResultFieldName = request.ResultFieldName ?? existing.ResultFieldName ?? string.Empty,
            ResultDataTypeName = request.ResultDataTypeName ?? existing.ResultDataTypeName
        };

        var calculationEntityType = request.CalculationEntityType ?? existing.CalculationEntityType;

        var typed = await ResolveTypedConfiguration(request, existing, calculationEntityType, ct).ConfigureAwait(false);
        if (typed.IsFailure)
        {
            return typed.ToNewResult<CalculationEntityDetailDto>();
        }

        var result = await _service.UpdateCalculation(
            request.Id,
            request.Name ?? existing.Name,
            request.Description ?? existing.Description,
            calculationEntityType,
            inputs,
            output,
            request.IsEnabled ?? existing.IsEnabled,
            typed.Value!,
            ct).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return result.ToNewResult<CalculationEntityDetailDto>();
        }

        return GenericResult<CalculationEntityDetailDto>.Success(
            MapToDetail(result.Value!));
    }

    // The implementation is the record a calculation is written as, so an update always writes one.
    // Sent: read it as the (possibly new) type's configuration. Omitted: keep the stored one, like every
    // other omitted field -- unless the type is changing, where the stored settings belong to the old type.
    private async Task<IGenericResult<IGenericConfiguration>> ResolveTypedConfiguration(
        UpdateCalculationEntityRequest request,
        CalculationEntityDetailDto existing,
        string calculationEntityType,
        CancellationToken ct)
    {
        if (request.Configuration is { } configuration)
        {
            return CalculationTypedConfigurationReader.Read(calculationEntityType, configuration, Logger);
        }

        if (!string.Equals(calculationEntityType, existing.CalculationEntityType, StringComparison.OrdinalIgnoreCase))
        {
            return GenericResult<IGenericConfiguration>.Failure(
                CalculationEndpointLog.TypedConfigurationRequiredForTypeChange(
                    Logger, existing.CalculationEntityType, calculationEntityType));
        }

        var current = await _service.GetCalculationById(request.Id, ct).ConfigureAwait(false);
        if (current.IsFailure)
        {
            return current.ToNewResult<IGenericConfiguration>();
        }

        return current.Value!.TypedConfiguration is { } stored
            ? GenericResult<IGenericConfiguration>.Success(stored)
            : GenericResult<IGenericConfiguration>.Failure(
                CalculationEndpointLog.TypedConfigurationUnreadable(
                    Logger, calculationEntityType, "the stored configuration", "the calculation has no stored configuration; send one"));
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
