using Fdw.Services.Data.Clients.Models;
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

namespace ReferenceDataSets.Endpoints;

/// <summary>
/// Endpoint to get fields for a DataSet.
/// Overrides base to project the field list to the app-specific DTO shape.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetDataSetFieldsEndpoint : GetDataSetFieldsEndpointBase
{
    // Why: Keep a typed reference so the override can call Get(name) and project
    // to the custom DTO shape required by this app.
    private readonly DataSetConfigurationProvider _dataSetProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetDataSetFieldsEndpoint"/> class.
    /// </summary>
    public GetDataSetFieldsEndpoint(DataSetConfigurationProvider dataSetProvider)
        : base(dataSetProvider)
    {
        _dataSetProvider = dataSetProvider;
    }

    /// <inheritdoc />
    protected override string Route => $"/{ResourceName}/{{Name}}/fields";

    /// <inheritdoc />
    protected override string EndpointSummary => "Get data set fields";

    /// <inheritdoc />
    protected override string EndpointDescription => "Returns all fields for a specific data set.";

    /// <inheritdoc />
    protected override async Task<IGenericResult<List<DataSetFieldPayload>?>> LoadDataSetFields(string dataSetName, CancellationToken ct)
    {
        var result = await _dataSetProvider.Get(dataSetName, ct).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value == null)
        {
            return GenericResult<List<DataSetFieldPayload>?>.Success((List<DataSetFieldPayload>?)null);
        }

        var fields = result.Value.Fields
            .OrderBy(f => f.Ordinal)
            .Select(f => new DataSetFieldPayload
            {
                Name = f.Name,
                DataType = f.TypeName,
                IsNullable = !f.IsRequired,
                IsKey = f.IsKey,
                Ordinal = f.Ordinal,
                Description = f.Description,
                Role = f.Role,
                IsJoinKey = f.IsJoinKey,
                CalculationName = f.CalculationName,
                IsCalculated = f.IsCalculated
            })
            .ToList();

        return GenericResult<List<DataSetFieldPayload>?>.Success((List<DataSetFieldPayload>?)fields);
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataSets");
    }
}
