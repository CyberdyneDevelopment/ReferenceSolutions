using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.ApiReference;
using Microsoft.AspNetCore.Http;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// The sign-in page: <c>/api/v1/scalar</c>, reading <c>/api/v1/openapi/sign-in</c>.
/// </summary>
/// <remarks>
/// Public. Above the auth routes sits a panel that signs the browser in on this host - one button per
/// external flow, a form per password flow, each naming its flow - and once signed in lists the
/// reference pages the session may open. See <see cref="SignInPanel"/>.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ApiReferencePages), "SignIn")]
public sealed class SignInReferencePage : ApiReferencePageBase
{
    /// <summary>Initializes a new instance of the <see cref="SignInReferencePage"/> class.</summary>
    public SignInReferencePage()
        : base(
            "SignIn",
            "/" + FdwReferenceEndpoints.Endpoints.RoutePrefix + "/scalar",
            "Sign in",
            requiresSignIn: false,
            requiredPolicies: [],
            headerContent: SignInPanel.Markup)
    {
    }

    /// <inheritdoc />
    public override string DocumentUrl(HttpContext context) => "/" + FdwReferenceEndpoints.Endpoints.RoutePrefix + "/openapi/sign-in";
}
