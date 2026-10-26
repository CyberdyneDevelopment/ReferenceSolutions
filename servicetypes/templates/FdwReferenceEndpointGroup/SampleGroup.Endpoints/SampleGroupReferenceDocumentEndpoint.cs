using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Web.RestEndpoints.ApiReference;
using Microsoft.Extensions.Logging;

namespace SampleGroup.Endpoints;

/// <summary>
/// The SampleGroup reference page's document: the group's routes, cut to what the caller may call: <c>GET /openapi/sample-permission</c>.
/// </summary>
/// <remarks>
/// Signed in is the only gate on the route; the cut is what keeps a caller to the operations they hold the right for.
/// SampleGroupReferenceDocumentOption is what declares the route; without it this class compiles and the document answers 404.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class SampleGroupReferenceDocumentEndpoint : ApiReferenceDocumentEndpointBase<EmptyRequest>
{
    /// <inheritdoc />
    public SampleGroupReferenceDocumentEndpoint(
        ILogger<ApiReferenceDocumentEndpointBase<EmptyRequest>> logger,
        ISystemRoleConfiguration systemRoles)
        : base(logger, systemRoles)
    {
    }

    /// <inheritdoc />
    protected override string DocumentName => FdwReferenceEndpoints.Endpoints.OpenApiDocumentName;

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/openapi/sample-permission");
        Description(builder => builder.ExcludeFromDescription());
    }

    /// <inheritdoc />
    protected override Task<IGenericResult> Filter(
        ApiReferenceDocument document,
        ApiReferenceCaller caller,
        EmptyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        // Why two segments: a Crud endpoint is routed at its resource name (the permission), a plain endpoint at the route prefix it was generated with.
        var routes = "/" + FdwReferenceEndpoints.Endpoints.RoutePrefix + "/";
        document.KeepPaths(path => Under(path, routes + "sample-permission") || Under(path, routes + "ROUTE_PREFIX"), Logger);
        document.SetTitle("SampleGroup");
        return Task.FromResult<IGenericResult>(document.KeepAdmitted(caller, Logger));
    }

    private static bool Under(string path, string route)
        => string.Equals(path, route, StringComparison.OrdinalIgnoreCase) || path.StartsWith(route + "/", StringComparison.OrdinalIgnoreCase);
}
