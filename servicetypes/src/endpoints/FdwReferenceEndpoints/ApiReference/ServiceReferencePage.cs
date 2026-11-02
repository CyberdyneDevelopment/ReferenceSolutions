using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.ApiReference;
using Microsoft.AspNetCore.Http;

namespace FdwReferenceEndpoints.ApiReference;

/// <summary>
/// The whole service surface: <c>/api/v1/reference/scalar</c>, reading <c>/api/v1/openapi/reference</c>.
/// </summary>
/// <remarks>
/// The successor of the <c>/scalar</c> page every host served. Anyone may open it; what it shows is
/// cut per caller - an anonymous visitor sees the operations that admit anonymous callers, a signed-in
/// caller every operation they hold the permission for.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ApiReferencePages), "ServiceReference")]
public sealed class ServiceReferencePage : ApiReferencePageBase
{
    /// <summary>Initializes a new instance of the <see cref="ServiceReferencePage"/> class.</summary>
    public ServiceReferencePage()
        : base(
            "ServiceReference",
            "/" + FdwReferenceEndpoints.Endpoints.RoutePrefix + "/reference/scalar",
            "API reference",
            requiresSignIn: false,
            requiredPolicies: [],
            headerContent: ReferencePagesNav.Markup)
    {
    }

    /// <inheritdoc />
    public override string DocumentUrl(HttpContext context) => "/" + FdwReferenceEndpoints.Endpoints.RoutePrefix + "/openapi/reference";
}
