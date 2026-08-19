using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ReferenceEndpoints.Extensions;

/// <summary>
/// Helpers for emitting consistent {errorCode, messages} JSON envelopes from any endpoint.
/// </summary>
/// <remarks>
/// API contract: every non-2xx response from an FDW endpoint should carry a body with
/// <c>errorCode</c> (camelCase, fixed vocabulary) and <c>messages</c> (string array).
/// Use these helpers in endpoint handlers instead of <c>Send.NotFoundAsync()</c>,
/// <c>Send.UnauthorizedAsync()</c>, etc., which emit empty bodies.
/// </remarks>
public static class ErrorEnvelopeExtensions
{
    /// <summary>Writes HTTP 404 with the standard NotFound envelope.</summary>
    [ExcludeFromCodeCoverage]
    public static Task WriteNotFound(this HttpContext context, string message, CancellationToken ct = default)
        => WriteErrorEnvelope(context, 404, "NotFound", new[] { message }, ct);

    /// <summary>Writes HTTP 401 envelope.</summary>
    [ExcludeFromCodeCoverage]
    public static Task WriteUnauthorized(this HttpContext context, string errorCode, string message, CancellationToken ct = default)
        => WriteErrorEnvelope(context, 401, errorCode, new[] { message }, ct);

    /// <summary>Writes HTTP 400 envelope.</summary>
    [ExcludeFromCodeCoverage]
    public static Task WriteBadRequest(this HttpContext context, string errorCode, string message, CancellationToken ct = default)
        => WriteErrorEnvelope(context, 400, errorCode, new[] { message }, ct);

    /// <summary>
    /// Maps an <see cref="IGenericResult"/> failure to the appropriate HTTP status + envelope.
    /// Recognizes "not found" in the result's message chain and emits 404; returns false otherwise
    /// (caller falls back to the 500 path).
    /// </summary>
    // Why: many service-layer Get/Update operations return IsSuccess=false with a "Not found"
    // message when the resource doesn't exist. Endpoints have been hard-coding 500 for any
    // failure, losing the distinction. This helper centralizes the mapping so callers get a
    // single line instead of repeated if/else blocks.
    [ExcludeFromCodeCoverage]
    public static async Task<bool> TryWriteFromResult(
        this HttpContext context,
        IGenericResult result,
        string resourceLabel,
        CancellationToken ct = default)
    {
        if (result is null) return false;
        if (result.IsSuccess) return false;

        var message = result.CurrentMessage ?? string.Empty;
        var isNotFound = LooksLikeNotFound(message)
            || result.Messages.Any(m => LooksLikeNotFound(m?.Message ?? string.Empty));

        if (isNotFound)
        {
            await context.WriteNotFound($"{resourceLabel} was not found.", ct).ConfigureAwait(false);
            return true;
        }

        return false;
    }

    private static bool LooksLikeNotFound(string text)
        => !string.IsNullOrEmpty(text)
            && text.Contains("not found", StringComparison.OrdinalIgnoreCase);

    private static Task WriteErrorEnvelope(
        HttpContext context,
        int statusCode,
        string errorCode,
        IEnumerable<string> messages,
        CancellationToken ct)
    {
        // Why RFC 7807 and not the {errorCode, messages[]} this used to emit: it was the fifth error
        // shape on one surface, and the last one left. ResultHttpStatusMapper, FastEndpoints'
        // validation failures and the CRUD not-found branch all speak ProblemDetails now, so a caller
        // could not parse failures uniformly while this helper kept answering in its own dialect.
        //
        // The vocabulary survives as extensions: errorCode is still there under `code`, and the
        // messages array is preserved rather than flattened, because callers pass more than one.
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = errorCode,
            Detail = string.Join(" ", messages),
            Instance = context.Request.Path.HasValue ? context.Request.Path.Value : null,
        };

        problem.Extensions["code"] = errorCode;
        problem.Extensions["messages"] = messages.ToArray();
        problem.Extensions["referenceId"] = context.TraceIdentifier;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        return context.Response.WriteAsJsonAsync(problem, ct);
    }
}
