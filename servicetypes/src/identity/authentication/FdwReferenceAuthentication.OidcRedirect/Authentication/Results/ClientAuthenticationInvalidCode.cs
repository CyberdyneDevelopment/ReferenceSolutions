using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceAuthentication.OidcRedirect.Authentication.Results;

/// <summary>
/// The properties declared for an OIDC client authentication type do not satisfy it.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(OidcClientAuthenticationResultCodes), "ClientAuthenticationInvalid", RestrictToCurrentCompilation = true)]
public sealed class ClientAuthenticationInvalidCode : OidcClientAuthenticationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClientAuthenticationInvalidCode"/> class.
    /// </summary>
    public ClientAuthenticationInvalidCode()
        : base(
            62100,
            "ClientAuthenticationInvalid",
            ResultSeverities.ByName("Error"),
            "OIDC client authentication properties are invalid: {ValidationErrors}",
            isRetryable: false)
    {
    }
}
