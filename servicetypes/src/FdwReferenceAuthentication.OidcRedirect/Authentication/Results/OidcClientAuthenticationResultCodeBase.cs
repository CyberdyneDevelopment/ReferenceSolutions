using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace FdwReferenceAuthentication.OidcRedirect.Authentication.Results;

/// <summary>
/// Base class for the result codes of OIDC client authentication.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class OidcClientAuthenticationResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected OidcClientAuthenticationResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance from a categorized <paramref name="number"/> (catalog scheme).
    /// </summary>
    protected OidcClientAuthenticationResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "OIDC", isRetryable)
    {
    }
}
