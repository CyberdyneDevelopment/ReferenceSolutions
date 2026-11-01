using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceAuth.Endpoints;

/// <summary>The sign-in flows a page may offer.</summary>
[ExcludeFromCodeCoverage]
public sealed class SignInFlowListResponse
{
    /// <summary>Gets or sets the flows, in configuration order.</summary>
    public IReadOnlyList<SignInFlowDto> Items { get; set; } = [];
}
