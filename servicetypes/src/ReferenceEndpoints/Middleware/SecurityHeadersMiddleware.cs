using System;
using System.Threading.Tasks;
using ReferenceEndpoints.Configuration;
using Microsoft.AspNetCore.Http;

namespace ReferenceEndpoints.Middleware;

/// <summary>Adds the standard security response headers, and suppresses caching on sensitive paths.</summary>
public sealed class SecurityHeadersMiddleware
{
    private static readonly string[] PermissionsPolicyValues =
    [
        "camera=()", "microphone=()", "geolocation=()",
        "payment=()", "usb=()", "magnetometer=()",
        "gyroscope=()", "accelerometer=()"
    ];

    private readonly RequestDelegate _next;
    private readonly SecurityHeadersOptions _options;

    /// <summary>Creates the middleware with the configured header policy.</summary>
    public SecurityHeadersMiddleware(RequestDelegate next, SecurityHeadersOptions options)
    {
        _next = next;
        _options = options;
    }

    /// <summary>Writes the headers, then runs the rest of the pipeline.</summary>
    public Task Invoke(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = _options.AllowFraming ? "SAMEORIGIN" : "DENY";
            headers["X-XSS-Protection"] = "0";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = string.Join(", ", PermissionsPolicyValues);

            if (!string.IsNullOrEmpty(_options.ContentSecurityPolicy))
            {
                headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;
            }
            else if (_options.EnableDefaultCsp)
            {
                headers["Content-Security-Policy"] = BuildDefaultCsp();
            }

            if (IsSensitivePath(context.Request.Path))
            {
                headers["Cache-Control"] = "no-store, no-cache, must-revalidate, proxy-revalidate";
                headers["Pragma"] = "no-cache";
                headers["Expires"] = "0";
            }

            return Task.CompletedTask;
        });

        return _next(context);
    }

    private static string BuildDefaultCsp()
    {
        var directives = new[]
        {
            "default-src 'self'",
            "script-src 'self' 'unsafe-inline'",
            "style-src 'self' 'unsafe-inline'",
            "img-src 'self' data: https:",
            "font-src 'self' https://fonts.gstatic.com",
            "connect-src 'self'",
            "frame-ancestors 'none'",
            "base-uri 'self'",
            "form-action 'self'"
        };
        return string.Join("; ", directives);
    }

    private bool IsSensitivePath(PathString path)
    {
        foreach (var sensitivePath in _options.SensitivePaths)
        {
            if (path.StartsWithSegments(sensitivePath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
