using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.ApiReference;
using FdwReferenceEndpoints.ApiReference;
using Microsoft.AspNetCore.Http;

namespace FdwReferenceWorkspaces.Endpoints;

/// <summary>
/// Every workspace the caller may read: <c>/api/v1/data/scalar</c>, reading <c>/api/v1/openapi/data</c>.
/// </summary>
/// <remarks>
/// Served to any signed-in caller, like the data route itself (FDW-937): the document lists only the
/// workspaces <c>MayRead</c> lets the caller query, so a member holding no platform permission -- a
/// vendor's key -- sees its own. Each workspace is a group of the page and each attached data set one operation.
/// A host may require policies of the page without subclassing it (<see cref="ApiReferencePagePolicies"/>); the
/// route, the pages list and <see cref="DataReferenceDocumentEndpoint"/> then admit the same callers, and the
/// per-workspace <c>MayRead</c> cut of what they see stays as it is.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ApiReferencePages), "DataReference")]
public sealed class DataReferencePage : ApiReferencePageBase
{
    /// <summary>Initializes a new instance of the <see cref="DataReferencePage"/> class.</summary>
    public DataReferencePage()
        : base(
            "DataReference",
            "/" + FdwReferenceEndpoints.Endpoints.RoutePrefix + "/data/scalar",
            "Data",
            requiresSignIn: true,
            requiredPolicies: [],
            headerContent: ReferencePagesNav.Markup)
    {
    }

    /// <inheritdoc />
    public override string DocumentUrl(HttpContext context) => "/" + FdwReferenceEndpoints.Endpoints.RoutePrefix + "/openapi/data";
}
