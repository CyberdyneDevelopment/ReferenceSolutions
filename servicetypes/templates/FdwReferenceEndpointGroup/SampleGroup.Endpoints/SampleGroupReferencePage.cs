using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.ApiReference;
using FdwReferenceEndpoints.ApiReference;
using Microsoft.AspNetCore.Http;

namespace SampleGroup.Endpoints;

/// <summary>
/// The SampleGroup API reference: <c>/api/v1/sample-permission/scalar</c>, reading <c>/api/v1/openapi/sample-permission</c>.
/// </summary>
/// <remarks>
/// Open to a signed-in caller only, because every route of the group states a right. The document it reads lists just the
/// operations that caller holds the right for. The list of pages the session may open sits above the reference.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ApiReferencePages), "SampleGroupReference")]
public sealed class SampleGroupReferencePage : ApiReferencePageBase
{
    /// <summary>Initializes a new instance of the <see cref="SampleGroupReferencePage"/> class.</summary>
    public SampleGroupReferencePage()
        : base(
            "SampleGroupReference",
            "/" + FdwReferenceEndpoints.Endpoints.RoutePrefix + "/sample-permission/scalar",
            "SampleGroup",
            requiresSignIn: true,
            requiredPolicies: [],
            headerContent: ReferencePagesNav.Markup)
    {
    }

    /// <inheritdoc />
    public override string DocumentUrl(HttpContext context) => "/" + FdwReferenceEndpoints.Endpoints.RoutePrefix + "/openapi/sample-permission";
}
