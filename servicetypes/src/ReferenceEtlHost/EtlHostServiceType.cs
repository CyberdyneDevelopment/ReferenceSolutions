using FastEndpoints;
using Microsoft.AspNetCore.Routing;
using Scalar.AspNetCore;
using Fdw.Collections;
using Fdw.Hosting.ApiHost;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.Security;

namespace ReferenceEtlHost;

/// <summary>
/// The etl server's host: the values this deployment supplies to the framework's API surface.
/// </summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "EtlHost")]
public class EtlHostServiceType : ApiHostServiceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EtlHostServiceType"/> class.
    /// </summary>
    public EtlHostServiceType()
        : base("EtlHost", "EtlHost", "Etl Host", "Fdw ETL Server - Pipeline execution and job management.")
    {
    }

    /// <inheritdoc />
    protected override string DocumentTitle => "Reference.Etl.Server";

    /// <inheritdoc />
    protected override string DocumentDescription => "Fdw ETL Server - Pipeline execution and job management.";

    /// <inheritdoc />
    /// <remarks>
    /// This server has endpoints deriving straight from FastEndpoints rather than an FDW base, and
    /// only the FDW bases add the permission pre-processor themselves. Registering it globally here
    /// is what makes Policies("resource:action") enforced on those too — without it they are
    /// declared and never checked, which fails open rather than loudly.
    /// </remarks>
    protected override void ConfigureEndpoints(Config config) =>
        config.Endpoints.Configurator = ep =>
            ep.PreProcessors(Order.Before, new PermissionClaimsPreProcessor());

    /// <inheritdoc />
    /// <remarks>
    /// Scalar reads the document the base generated; its route has to match the pattern
    /// UseSwaggerGen serves it on, which is why the two are stated together rather than left to
    /// defaults that drift apart.
    /// </remarks>
    protected override void MapEndpoints(IEndpointRouteBuilder routes) =>
        routes.MapScalarApiReference(options =>
            options.WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json"));
}
