using System.Diagnostics.CodeAnalysis;
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
        return CalculationEntityDetailMapper.Found(await _service.GetCalculationById(request.Id, ct).ConfigureAwait(false));
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
        var inputs = CalculationEntityInputMapper.ToInputs(request.Inputs ?? existing.Inputs, Logger);
        if (!inputs.IsSuccess || inputs.Value is not { } mappedInputs)
        {
            return inputs.ToNewResult<CalculationEntityDetailDto>();
        }

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
            mappedInputs,
            output,
            request.IsEnabled ?? existing.IsEnabled,
            typed.Value!,
            ct).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return result.ToNewResult<CalculationEntityDetailDto>();
        }

        return GenericResult<CalculationEntityDetailDto>.Success(
            CalculationEntityDetailMapper.ToDetail(result.Value!));
    }

    // The implementation is the record a calculation is written as, so an update always writes one.
    // Sent with the type unchanged: each member sent replaces the stored one and each member not sent keeps it, like every
    // other field of an update. Sent with the type changing: read as the new type's whole configuration, because the stored
    // settings belong to the old type. Omitted: keep the stored one, unless the type is changing.
    private async Task<IGenericResult<IGenericConfiguration>> ResolveTypedConfiguration(
        UpdateCalculationEntityRequest request,
        CalculationEntityDetailDto existing,
        string calculationEntityType,
        CancellationToken ct)
    {
        var typeChanges = !string.Equals(calculationEntityType, existing.CalculationEntityType, StringComparison.OrdinalIgnoreCase);
        if (request.Configuration is { } configuration && typeChanges)
        {
            return CalculationTypedConfigurationReader.Read(calculationEntityType, configuration, Logger);
        }

        if (typeChanges)
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

        if (current.Value!.TypedConfiguration is not { } stored)
        {
            return request.Configuration is { } whole
                ? CalculationTypedConfigurationReader.Read(calculationEntityType, whole, Logger)
                : GenericResult<IGenericConfiguration>.Failure(
                    CalculationEndpointLog.TypedConfigurationUnreadable(
                        Logger, calculationEntityType, "the stored configuration", "the calculation has no stored configuration; send one"));
        }

        return request.Configuration is { } sent
            ? CalculationTypedConfigurationReader.ReadOver(calculationEntityType, stored, sent, Logger)
            : GenericResult<IGenericConfiguration>.Success(stored);
    }
}
