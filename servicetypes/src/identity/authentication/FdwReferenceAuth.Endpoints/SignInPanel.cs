using System.Diagnostics.CodeAnalysis;
using FdwReferenceEndpoints.ApiReference;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// The sign-in the reference page offers: a Sign in button at the top right that opens the sign-in as a
/// dialog in the middle of the page, and beside it, while signed in, a Sign out button.
/// </summary>
/// <remarks>
/// <para>
/// It signs a browser in on this host the way the console's login screen does, naming a flow on every
/// sign-in and hardcoding none: it asks <c>/auth/sign-in-flows</c> which flows admit this page, draws a
/// button per external flow (redirect, then <c>/auth/redeem</c> with the returned <c>auth_code</c>) and
/// a form per password flow (<c>/auth/login</c>, with the change-password form when the answer says a
/// change is required). Signed in, it lists the reference pages the session may open, read from
/// <c>/reference/pages</c>.
/// </para>
/// <para>
/// API keys never sign in here. A key is sent as <c>Authorization: Bearer {prefix}…</c> on each request;
/// on the data references it goes in the page's own Authentication box for "Try it" calls.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public static class SignInPanel
{
    /// <summary>Gets the panel's HTML, style and script.</summary>
    public static string Markup { get; } =
        "<style>" + SignInPanelStyle.Css + ReferencePagesNav.Css + "</style>"
        + "<div id=\"fdw-session\"><button type=\"button\" id=\"fdw-signin-open\">Sign in</button>"
        + "<button type=\"button\" id=\"fdw-signout\" hidden>Sign out</button></div>"
        + ReferencePagesNav.ListMarkup
        + "<dialog id=\"fdw-signin\" aria-labelledby=\"fdw-signin-title\" aria-live=\"polite\">"
        + "<button type=\"button\" id=\"fdw-signin-close\" aria-label=\"Close\">&times;</button>"
        + "<h2 id=\"fdw-signin-title\">Sign in</h2>"
        + "<p id=\"fdw-signin-message\" role=\"status\"></p><div id=\"fdw-signin-body\"></div></dialog>"
        + "<script>" + SignInPanelScript.For("/" + FdwReferenceEndpoints.Endpoints.RoutePrefix) + "</script>";
}
