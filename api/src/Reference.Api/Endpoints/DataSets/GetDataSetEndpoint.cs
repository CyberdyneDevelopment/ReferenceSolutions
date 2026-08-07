using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using Fdw.Services.Data.Clients.Models;

namespace Reference.Api.Endpoints;

/// <summary>
/// Endpoint to get a DataSet by name.
/// Overrides base to add field-shape projection while delegating the provider lookup to the base.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetDataSetEndpoint : GetDataSetEndpointBase
{
    // Why: Keep a typed reference to the concrete provider so this override can call Get(name)
    // directly and project the result to the richer DTO shape required by this app.
    private readonly DataSetConfigurationProvider _dataSetProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetDataSetEndpoint"/> class.
    /// </summary>
    public GetDataSetEndpoint(DataSetConfigurationProvider dataSetProvider)
        : base(dataSetProvider)
    {
        _dataSetProvider = dataSetProvider;
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataSetDetailResponse?>> LoadDataSetDetail(string name, CancellationToken ct)
    {
        var result = await _dataSetProvider.Get(name, ct).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value == null)
        {
            return GenericResult<DataSetDetailResponse?>.Success((DataSetDetailResponse?)null);
        }

        var ds = result.Value;
        var detail = new DataSetDetailResponse
        {
            Name = ds.Name,
            Description = ds.Description,
            Category = ds.Category,
            Version = ds.Version,
            RecordTypeName = ds.RecordTypeName,
            SurrogateKeyFields = ds.KeyFields
                .Where(k => string.Equals(k.KeyType, "Surrogate", System.StringComparison.OrdinalIgnoreCase))
                .OrderBy(k => k.Ordinal)
                .Select(k => k.KeyName)
                .ToList(),
            NaturalKeyFields = ds.KeyFields
                .Where(k => string.Equals(k.KeyType, "Natural", System.StringComparison.OrdinalIgnoreCase))
                .OrderBy(k => k.Ordinal)
                .Select(k => k.KeyName)
                .ToList(),
            Fields = ds.Fields.Select(f => new DataSetFieldResponse
            {
                Name = f.Name,
                Description = f.Description,
                DataType = f.TypeName,
                IsKey = f.IsKey,
                IsRequired = f.IsRequired,
                MaxLength = f.MaxLength,
                IsCalculated = f.IsCalculated,
                CalculationName = f.CalculationName,
                IsJoinKey = f.IsJoinKey,
                Role = f.Role,
                Ordinal = f.Ordinal
            }).ToList(),
            Filters = new List<DataSetFilterConditionPayload>(),
            CreatedAt = System.DateTime.UtcNow,
            CreatedBy = string.Empty,
            ModifiedBy = string.Empty,
            CreatedOnBehalfOf = string.Empty,
            ModifiedOnBehalfOf = string.Empty
        };

        return GenericResult<DataSetDetailResponse?>.Success((DataSetDetailResponse?)detail);
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataSets");
    }
}
