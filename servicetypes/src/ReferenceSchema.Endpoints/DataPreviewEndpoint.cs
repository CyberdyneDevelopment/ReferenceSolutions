using System.Diagnostics.CodeAnalysis;
using Fdw.Schema.Endpoints.Discovery;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ReferenceSchema.Endpoints;

/// <summary>
/// POST /schema/preview — thin subclass of <see cref="DataPreviewEndpointBase"/>.
/// All logic is in the base; this class only pins the route, policy, and tag.
/// </summary>
[ExcludeFromCodeCoverage]
public class DataPreviewEndpoint : DataPreviewEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataPreviewEndpoint"/> class.
    /// </summary>
    public DataPreviewEndpoint(IDataGateway dataGateway, ILogger<DataPreviewEndpoint> logger)
        : base(dataGateway, logger ?? NullLogger<DataPreviewEndpoint>.Instance)
    {
    }

    // Why: keep the route the UI and Newman collection already post to.
    protected override string Route => "/schema/preview";

    // Why: preview lets the caller read raw rows from any connection — write-class capability.
    // Viewer is denied (403); Operator and Admin pass. Matches the original endpoint's policy.
    protected override string PolicyName => "datastores:write";

    // Why: add the Schema OpenAPI tag so this endpoint appears in the Schema group in Scalar.
    protected override void OnBeforeConfiguring() => Tags("Schema");
}
