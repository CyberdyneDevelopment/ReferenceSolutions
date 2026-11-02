using FastEndpoints;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Microsoft.AspNetCore.Http;

namespace FdwReferenceEndpoints.Extensions;

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

    private static async Task WriteErrorEnvelope(
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
        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = statusCode,
            Title = errorCode,
            Detail = string.Join(" ", messages),
            Instance = context.Request.Path.HasValue ? context.Request.Path.Value : null,
        };

        problem.Extensions["code"] = errorCode;
        problem.Extensions["messages"] = messages.ToArray();
        problem.Extensions["referenceId"] = context.TraceIdentifier;

        // Through FastEndpoints' sender, not a raw write: it marks the response as sent, which is
        // what stops the calling endpoint from sending again. TypedResults.Problem writes
        // application/problem+json with the status the problem carries.
        await context.Response.SendResultAsync(TypedResults.Problem(problem)).ConfigureAwait(false);
    }
}
