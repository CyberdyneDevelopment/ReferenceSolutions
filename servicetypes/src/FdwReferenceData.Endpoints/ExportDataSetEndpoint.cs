using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Delimited.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Results;
using Fdw.Services.Data.Endpoints;
using Fdw.Web.RestEndpoints.Pagination;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>
/// GET /datasets/{Name}/export — streams every row of a DataSet in the format the caller asks for,
/// instead of materialising the rows and sending one body.
/// </summary>
/// <remarks>
/// This is the worked example of <see cref="StreamRecordsEndpointBase{TRequest}"/>: the endpoint
/// supplies a cursor and the format its callers read, the host supplies the limits through its
/// ResponseStreaming configuration, and the base owns negotiation, the response pump, the
/// concurrency cap and how an incomplete response is reported.
/// <para>
/// Paging belongs to <c>/datasets/{name}/query</c>, which is the right shape for a grid. This
/// endpoint is for taking the whole thing.
/// </para>
/// </remarks>
public class ExportDataSetEndpoint : StreamRecordsEndpointBase<ExportDataSetRequest>
{
    private readonly IDataSetConfigurationProvider _dataSetProvider;
    private readonly IDataGatewayProvider _dataGateways;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportDataSetEndpoint"/> class.
    /// </summary>
    public ExportDataSetEndpoint(
        IDataSetConfigurationProvider dataSetProvider,
        IDataGatewayProvider dataGateways,
        ResponseStreamingOptions streaming,
        ILogger<ExportDataSetEndpoint> logger)
        : base(streaming, logger ?? NullLogger<ExportDataSetEndpoint>.Instance)
    {
        _dataSetProvider = dataSetProvider;
        _dataGateways = dataGateways;
    }

    /// <inheritdoc />
    protected override string ResourceName => "datasets";

    /// <inheritdoc />
    protected override string Route => "/datasets/{Name}/export";

    // Why not datasets:read: reading a page is not taking the whole set. A bulk export is the shape
    // of an exfiltration, and /schema/preview already sits on a stricter policy than read for the
    // same reason.
    /// <inheritdoc />
    protected override string PolicyName => "datasets:export";

    // Why Json: this endpoint's callers are the UI and other services, which read JSON. A client
    // wanting a spreadsheet asks for text/csv and the base serves it from the same cursor.
    /// <inheritdoc />
    protected override string DefaultFormat => "Json";

    /// <inheritdoc />
    protected override string EndpointSummary => "Export a data set";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Streams every row of the data set progressively, in the format named by the Accept header " +
        $"(application/json, text/csv or the Excel workbook type) or by a format query value (Json, Delimited or Xlsx), which wins over Accept and is for a link a browser follows. The outcome is reported in the {StatusTrailerName} trailer " +
        "where the protocol carries trailers; an incomplete response always ends as a truncated body.";

    /// <inheritdoc />
    protected override string ResourceIdentifier(ExportDataSetRequest request) => request.Name;

    /// <inheritdoc />
    protected override void ConfigureEndpoint() => Tags("DataSets");

    // Why on here and not in the format's defaults (FDW-1298): this endpoint's CSV is a file someone opens in a spreadsheet. The
    // byte order mark is what makes Excel read it as UTF-8, and the formula guard stops a text cell that starts with = + - @ from
    // being run as a formula. Both change the bytes a machine reader sees, so the delimited format leaves them off for every other
    // use of it. A caller wanting the data unchanged asks for Json.
    /// <inheritdoc />
    protected override RowWriterOptions WriterOptions(IRecordWriterType format, IReadOnlyList<string> columns)
    {
        var options = base.WriterOptions(format, columns);
        if (options is DelimitedRowWriterOptions delimited)
        {
            delimited.WriteByteOrderMark = true;
            delimited.NeutralizeFormulas = true;
        }

        return options;
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<IRecordSource<DataRecord>>> OpenRecords(
        ExportDataSetRequest request, CancellationToken cancellationToken)
    {
        var dataSet = await _dataSetProvider.Get(request.Name, cancellationToken).ConfigureAwait(false);
        if (dataSet.IsFailure)
        {
            return dataSet.ToNewResult<IRecordSource<DataRecord>>();
        }

        if (dataSet.Value is null)
        {
            return GenericResult<IRecordSource<DataRecord>>.Failure(
                DataServiceResultCodes.ByName("DataSetNotFound"), Logger, ResultDetails.Create("DataSetName", request.Name));
        }

        // Why through DataSetRecordSource (FDW-1075): an export streams the data set's own rows -- its renames,
        // transformations, calculated fields and only its declared fields -- page by page through the same read as
        // POST /data/{name}. Opening the first source's container streamed every column of the table under the data
        // set, raw. Pages are the batch size the paging settings give (FDW-1209: not Resolve's take, which an API key no
        // limit applies to would have been handed as the largest there is); the row ceiling stays the base's MaxRows.
        var batchSize = PaginationQuery.ResolveBatchSize(HttpContext, Logger);
        if (batchSize.IsFailure)
            return batchSize.ToNewResult<IRecordSource<DataRecord>>();

        var rowCap = PaginationQuery.ResolveCalculatedRowCap(HttpContext, Logger);
        if (rowCap.IsFailure)
            return rowCap.ToNewResult<IRecordSource<DataRecord>>();

        return DataSetRecordSource.Open(new DataSetQuery(_dataGateways, Logger), dataSet.Value, batchSize.Value, rowCap.Value, Logger);
    }
}
