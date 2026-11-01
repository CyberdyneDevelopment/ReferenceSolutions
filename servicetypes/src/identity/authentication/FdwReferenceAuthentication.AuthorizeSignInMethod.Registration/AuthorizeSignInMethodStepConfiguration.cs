using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceAuthentication.AuthorizeSignInMethod.Registration;

/// <summary>
/// What an AuthorizeSignInMethod step row's Configuration column holds.
/// </summary>
/// <remarks>
/// <code>{ "RequiredPermission": "signin:password" }</code>
/// One permission per step row. A flow that should admit only holders of two permissions names the
/// step twice; a flow that should admit everyone omits the step, so "open" is never a value here.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed record AuthorizeSignInMethodStepConfiguration
{
    /// <summary>Gets the permission a principal must hold, among the permissions baked for it, to be issued anything.</summary>
    public string? RequiredPermission { get; init; }
}
