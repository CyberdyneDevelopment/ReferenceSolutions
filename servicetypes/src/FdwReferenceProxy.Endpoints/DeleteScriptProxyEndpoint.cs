using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Delete a stored script (proxy). Forwards the delete to the ETL server.</summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteScriptProxyEndpoint : ScriptProxyEndpointBase
{
    /// <inheritdoc />
    public DeleteScriptProxyEndpoint(IHttpClientFactory factory, ILogger<DeleteScriptProxyEndpoint> logger)
        : base(factory, logger)
    {
    }

    /// <inheritdoc />
    protected override string RequiredPolicy => "scripts:delete";

    /// <inheritdoc />
    protected override System.Net.Http.HttpMethod Method => System.Net.Http.HttpMethod.Delete;

    /// <inheritdoc />
    protected override string TargetPath() => $"scripts/{Route<string>("Name")}";

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/scripts/{Name}");
        ConfigureScripts("Delete a stored script (proxy)", "Forwards the delete to the ETL server.");
    }
}
