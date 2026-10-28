using System;
using System.Text.RegularExpressions;
using FastEndpoints;

namespace FdwReferenceProxy.Endpoints;

/// <summary>One route that reference-api relays to the ETL server: how a caller reaches it here, what the caller needs, and where it goes.</summary>
/// <param name="Verb">The HTTP verb, the same on both hosts.</param>
/// <param name="Route">The route on reference-api, with <c>{name}</c> placeholders for its segments.</param>
/// <param name="Policy">The permission the caller needs, as the ETL endpoint declares it.</param>
/// <param name="Upstream">The route on the ETL server, relative to its base address, with the same <c>{name}</c> placeholders as <paramref name="Route"/>.</param>
/// <param name="Summary">The one-line summary of the route in the API reference.</param>
/// <param name="Description">What the route does, for the API reference.</param>
/// <param name="Tag">The group the route is listed under in the API reference.</param>
public sealed record EtlRelay(Http Verb, string Route, string Policy, string Upstream, string Summary, string Description, string Tag)
{
    private static readonly Regex Placeholder = new(@"\{(?<name>[^}]+)\}", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));

    /// <summary>Gets the HTTP method the call is forwarded with.</summary>
    public System.Net.Http.HttpMethod Method => new(Verb.ToString());

    /// <summary>Fills the placeholders of <see cref="Upstream"/> with the route values of the call, each escaped for a path segment.</summary>
    /// <param name="routeValue">Reads a route value by the placeholder's name.</param>
    /// <returns>The route to call on the ETL server.</returns>
    /// <exception cref="InvalidOperationException">A placeholder names a segment the call does not have. The route and the upstream route are declared together, so this is a mistake in the declaration and is not answered with a made-up value.</exception>
    public string UpstreamFor(Func<string, string?> routeValue)
    {
        ArgumentNullException.ThrowIfNull(routeValue);
        return Placeholder.Replace(Upstream, match =>
        {
            var name = match.Groups["name"].Value;
            var value = routeValue(name);
            if (value is null)
            {
                throw new InvalidOperationException($"The upstream route '{Upstream}' of '{Route}' names the segment '{name}', which the call does not have.");
            }

            return Uri.EscapeDataString(value);
        });
    }
}
