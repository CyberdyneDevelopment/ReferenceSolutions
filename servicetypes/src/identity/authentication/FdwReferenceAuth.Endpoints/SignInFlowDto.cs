using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceAuth.Endpoints;

/// <summary>One sign-in flow a page may offer.</summary>
[ExcludeFromCodeCoverage]
public sealed class SignInFlowDto
{
    /// <summary>Gets or sets the flow's name; the page sends it back as <c>flowName</c>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets what the page labels the flow with: the identity provider's name for an external flow, the flow's name for a password flow.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets how the flow signs a caller in: <c>external</c> (a redirect to an identity provider) or <c>password</c> (a username and password form).</summary>
    public string Kind { get; set; } = string.Empty;
}
