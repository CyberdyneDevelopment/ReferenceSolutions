using System;
using System.Collections.Generic;

namespace ReferenceHosting.Middleware;

/// <summary>
/// The routes a host considers legitimately body-less, for <see cref="EmptyBodyBadRequestMiddleware"/>.
/// </summary>
/// <remarks>
/// Why this is configuration rather than a constant: which routes may arrive without a body is a
/// property of the host's own surface, not of the rule. A package that hardcoded one application's
/// paths would be that application's middleware wearing a package's name.
/// </remarks>
public sealed class EmptyBodyOptions
{
    /// <summary>
    /// Exact paths that may be called with no body — an operation acting purely on server-side state,
    /// such as revoking the token presented in the Authorization header.
    /// </summary>
    public IReadOnlyList<string> BodylessPaths { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Path shapes that may be called with no body, matched as prefix plus suffix — an operation
    /// naming a resource and an action, such as triggering a configured pipeline.
    /// </summary>
    public IReadOnlyList<BodylessRoute> BodylessRoutes { get; init; } = Array.Empty<BodylessRoute>();

    /// <summary>
    /// Determines whether <paramref name="path"/> is declared body-less by this host.
    /// </summary>
    /// <param name="path">The request path.</param>
    /// <returns><see langword="true"/> when the path is allowed to arrive without a body.</returns>
    public bool Allows(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        foreach (var allowed in BodylessPaths)
        {
            if (string.Equals(path, allowed, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        foreach (var route in BodylessRoutes)
        {
            if (route.Matches(path))
                return true;
        }

        return false;
    }
}

/// <summary>
/// A body-less route shape: a path that starts with <see cref="Prefix"/> and ends with
/// <see cref="Suffix"/>, with a resource identifier between them.
/// </summary>
/// <param name="Prefix">The leading path segment, for example <c>/api/v1/pipelines/</c>.</param>
/// <param name="Suffix">The trailing action segment, for example <c>/execute</c>.</param>
// Why a record rather than parallel arrays: a prefix and its suffix are one fact. Held apart in two
// lists they can be edited out of step, and the resulting mismatch is silent — a route that matches
// the wrong action, or none.
public sealed record BodylessRoute(string Prefix, string Suffix)
{
    /// <summary>Determines whether <paramref name="path"/> matches this shape.</summary>
    /// <param name="path">The request path.</param>
    /// <returns><see langword="true"/> when both the prefix and the suffix match.</returns>
    public bool Matches(string path)
        => path.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
        && path.EndsWith(Suffix, StringComparison.OrdinalIgnoreCase);
}
