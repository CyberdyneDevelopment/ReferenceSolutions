using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using ReferenceAuthentication.OpenIddict.Claims;

namespace ReferenceAuth.Endpoints;

/// <summary>
/// Alias endpoint for tenant-scoped token re-mint via the OpenIddict pipeline.
/// The UI posts <c>grant_type=refresh_token&amp;tenant=&lt;id&gt;</c> here; this endpoint
/// forwards the request body verbatim to <c>/connect/token</c> on the same server so
/// the OpenIddict engine processes it with the tenant parameter intact.
///
/// Route: POST /auth/token-switch
/// </summary>
/// <remarks>
/// Why a proxy rather than a direct redirect: the UI's auth client is wired to
/// <c>/auth/token-switch</c>. Keeping that route avoids a UI code change while the
/// canonical OpenIddict endpoint remains <c>/connect/token</c>.
/// </remarks>
[ExcludeFromCodeCoverage]
public class TokenSwitchEndpoint : EndpointWithoutRequest
{
    private readonly IHttpClientFactory _httpClientFactory;

    /// <inheritdoc />
    public TokenSwitchEndpoint(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/auth/token-switch");
        AllowAnonymous();
        // Why: The caller sends grant_type=refresh_token + tenant param as a form body;
        // OpenIddict validates the token and requires Content-Type: application/x-www-form-urlencoded.
        AllowFormData();
        Tags("Auth");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        // Forward form body to /connect/token on this server.
        // Why: IHttpClientFactory "TokenSwitch" is registered with BaseAddress = server's own URL
        // so the request goes through the full OpenIddict pipeline including ProcessSignInClaimsHandler.
        var client = _httpClientFactory.CreateClient("TokenSwitch");

        // Copy all form fields from the incoming request.
        var form = HttpContext.Request.Form;
        var formContent = new FormUrlEncodedContent(
            form.SelectMany(kvp => kvp.Value.Select(v => KeyValuePair.Create(kvp.Key, v ?? string.Empty))));

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync("connect/token", formContent, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ThrowError($"Token switch request failed: {ex.Message}");
            return; // unreachable; ThrowError throws
        }

        // Return the OpenIddict response (JSON body) with the same status code.
        HttpContext.Response.StatusCode = (int)response.StatusCode;
        HttpContext.Response.ContentType = response.Content.Headers.ContentType?.ToString()
            ?? "application/json";

        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        await HttpContext.Response.WriteAsync(body, ct).ConfigureAwait(false);
    }
}
