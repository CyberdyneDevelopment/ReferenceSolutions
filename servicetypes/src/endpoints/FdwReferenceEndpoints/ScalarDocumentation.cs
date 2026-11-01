using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Fdw.UI.Themes.Configuration;
using Fdw.Web.Http.Authentication;
using Fdw.Web.RestEndpoints.ApiReference;
using Fdw.Web.RestEndpoints.ApiReference.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Scalar.AspNetCore;

using FdwReferenceEndpoints.Configuration;
namespace FdwReferenceEndpoints;

/// <summary>
/// The API reference pages this surface serves, themed from the configured palette.
/// </summary>
/// <remarks>
/// <para>
/// Here rather than in a host because what the reference pages show is a property of the endpoint
/// surface. A host used to carry ninety lines of inline CSS and JavaScript to do it.
/// </para>
/// <para>
/// Every page registered in <see cref="ApiReferencePages"/> is mapped, each loading its own document
/// from a document endpoint that cuts it for the caller (FDW-919). The whole OpenAPI document is no
/// longer served: NSwag's middleware (<c>UseSwaggerGen</c>) served one document to every caller - the
/// first caller's - along with a public Swagger UI, so it is not mapped at all.
/// </para>
/// </remarks>
internal static class ScalarDocumentation
{
    /// <summary>Serves every registered reference page and the redirect to the host's own.</summary>
    /// <param name="app">The application to map onto.</param>
    /// <param name="configuration">Where the root redirects to, and the theme's fallbacks.</param>
    /// <param name="loggerFactory">Creates the logger; null when the host supplies none.</param>
    internal static void Map(WebApplication app, ApiReferenceHostConfiguration configuration, ILoggerFactory? loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(configuration);

        var logger = loggerFactory?.CreateLogger(nameof(ScalarDocumentation)) ?? NullLogger.Instance;
        var head = BuildHead(app, configuration);

        app.MapGet("/", () => Results.Redirect(configuration.RoutePath)).ExcludeFromDescription();

        foreach (var page in ApiReferencePages.All())
        {
            var endpoint = app.MapScalarApiReference(page.Route, (options, context) =>
            {
                options.WithTitle(page.Title);
                options.WithOpenApiRoutePattern(page.DocumentUrl(context));
                options.WithTheme(ScalarTheme.None);
                options.AddHeadContent(head);
                if (page.HeaderContent.Length > 0)
                    options.AddHeaderContent(page.HeaderContent);
                SignInTryIt(options, context);
            });

            // A page that needs no sign-in is still cut per caller: its document shows an anonymous
            // caller only what admits anonymous callers.
            if (page.RequiresSignIn)
            {
                var policies = ApiReferencePagePolicies.For(page, app.Configuration);
                endpoint.RequireAuthorization(policies.ToArray());
                ApiReferenceLog.PageMapped(logger, page.Route, page.Title,
                    "signed in, holding [" + string.Join(", ", policies) + "]");
            }
            else
            {
                endpoint.AllowAnonymous();
                ApiReferenceLog.PageMapped(logger, page.Route, page.Title, "anyone");
            }
        }
    }

    /// <summary>The security scheme FastEndpoints declares on every operation, which "Try it" fills in.</summary>
    private const string BearerScheme = "JWTBearerAuth";

    // Why a token in the page: every operation declares the bearer scheme, and "Try it" sends only what that
    // scheme holds. A browser signed in on these pages holds a session cookie, which Try it neither presents as
    // that scheme nor accompanies with the CSRF header a session write needs. So a signed-in caller's page carries
    // a short-lived token this host mints for that session, asserting exactly what the session asserts (FDW-1100).
    // A caller with no session gets no token and fills the box as before. The page is then a credential, so it is
    // never cached.
    private static void SignInTryIt(ScalarOptions options, HttpContext context)
    {
        if (context.RequestServices.GetService<IAccessTokenProvider>() is not { } tokens)
            return;

#pragma warning disable VSTHRD002 // Scalar's options callback is synchronous; this waits for one token to be signed.
        var token = tokens.GetAccessToken(context.RequestAborted).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        if (string.IsNullOrEmpty(token))
            return;

        context.Response.Headers.CacheControl = "no-store";
        options.AddPreferredSecuritySchemes([BearerScheme])
            .AddHttpAuthentication(BearerScheme, scheme => scheme.WithToken(token));
    }

    private static string BuildHead(WebApplication app, ApiReferenceHostConfiguration configuration)
    {
        var themesResult = app.Services.GetRequiredService<Fdw.UI.Themes.ThemeConfigurationProvider>().Get().GetAwaiter().GetResult();
        var themes = themesResult.IsSuccess ? themesResult.Value ?? [] : [];
        var defaultTheme = themes.FirstOrDefault(t => t.IsDefault)
            ?? themes.FirstOrDefault(t => string.Equals(t.Name, "fractal", StringComparison.OrdinalIgnoreCase))
            ?? (themes.Count > 0 ? themes[0] : null);

        var tenantsJs = BuildTenantsJs(themes);
        var defaultCss = defaultTheme != null ? BuildScalarCss(defaultTheme) : string.Empty;

        return $@"
                <style>
                    :root {{
                        {defaultCss}
                        --scalar-font-code: '{EscapeJs(defaultTheme?.FontFamilyMono ?? configuration.FallbackFontFamilyMono)}';
                        --scalar-border-radius: {(defaultTheme?.BorderRadius ?? configuration.FallbackBorderRadius).ToString(CultureInfo.InvariantCulture)}px;
                    }}
                    .sidebar {{ border-right: 1px solid var(--scalar-border-color); }}
                </style>
                <script>
                    const themes = {tenantsJs};
                    (function applyTheme() {{
                        const urlParams = new URLSearchParams(window.location.search);
                        const hostname = window.location.hostname.split('.')[0];
                        const tenantKey = urlParams.get('tenant') || (themes[hostname] ? hostname : '{EscapeJs(defaultTheme?.Name ?? configuration.FallbackThemeName)}');
                        const theme = themes[tenantKey] || themes['{EscapeJs(defaultTheme?.Name ?? configuration.FallbackThemeName)}'];
                        if (!theme) return;
                        document.title = theme.title || 'API Reference';
                        const root = document.documentElement;
                        if (theme.colors) {{
                            for (const [key, value] of Object.entries(theme.colors)) {{
                                root.style.setProperty(key, value);
                            }}
                        }}
                        if (theme.logoUrl) root.style.setProperty('--scalar-custom-logo', `url('${{theme.logoUrl}}')`);
                        if (theme.fontUrl) {{
                            const link = document.createElement('link');
                            link.rel = 'stylesheet'; link.href = theme.fontUrl;
                            document.head.appendChild(link);
                        }}
                        if (theme.font) root.style.setProperty('--scalar-font', theme.font);
                    }})();
                </script>";
    }

    private static string BuildScalarCss(IThemeImplementationConfiguration theme)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-color-accent: {theme.PrimaryColor};");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-button-1: {theme.PrimaryColor};");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-button-1-color: {theme.TextPrimary};");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-background-1: {theme.BackgroundColor};");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-background-2: {theme.SurfaceColor};");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-background-3: {theme.SurfaceColor};");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-color-1: {theme.TextPrimary};");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-color-2: {theme.TextSecondary};");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-border-color: {theme.TextSecondary}33;");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-color-green: {theme.SuccessColor};");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-color-red: {theme.ErrorColor};");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-color-orange: {theme.WarningColor};");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-color-blue: {theme.InfoColor};");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-font: '{EscapeJs(theme.FontFamily)}';");
        return sb.ToString();
    }

    private static string BuildTenantsJs(IReadOnlyList<IThemeImplementationConfiguration> themes)
    {
        if (themes.Count == 0) return "{}";
        var sb = new StringBuilder();
        sb.Append('{');
        for (int i = 0; i < themes.Count; i++)
        {
            var t = themes[i];
            if (i > 0) sb.Append(',');
            sb.Append(CultureInfo.InvariantCulture, $@"
                ""{EscapeJs(t.Name)}"": {{
                    ""title"": ""{EscapeJs(t.DisplayName ?? t.AppName ?? t.Name)} API"",
                    ""logoUrl"": {(t.LogoUrl != null ? $@"""{EscapeJs(t.LogoUrl)}""" : "null")},
                    ""fontUrl"": null,
                    ""font"": ""{EscapeJs(t.FontFamily)}"",
                    ""colors"": {{
                        ""--scalar-color-accent"": ""{EscapeJs(t.PrimaryColor)}"",
                        ""--scalar-button-1"": ""{EscapeJs(t.PrimaryColor)}"",
                        ""--scalar-button-1-color"": ""{EscapeJs(t.TextPrimary)}"",
                        ""--scalar-background-1"": ""{EscapeJs(t.BackgroundColor)}"",
                        ""--scalar-background-2"": ""{EscapeJs(t.SurfaceColor)}"",
                        ""--scalar-background-3"": ""{EscapeJs(t.SurfaceColor)}"",
                        ""--scalar-color-1"": ""{EscapeJs(t.TextPrimary)}"",
                        ""--scalar-color-2"": ""{EscapeJs(t.TextSecondary)}"",
                        ""--scalar-border-color"": ""{EscapeJs(t.TextSecondary)}33"",
                        ""--scalar-color-green"": ""{EscapeJs(t.SuccessColor)}"",
                        ""--scalar-color-red"": ""{EscapeJs(t.ErrorColor)}"",
                        ""--scalar-color-orange"": ""{EscapeJs(t.WarningColor)}"",
                        ""--scalar-color-blue"": ""{EscapeJs(t.InfoColor)}""
                    }}
                }}");
        }
        sb.Append('}');
        return sb.ToString();
    }

    private static string EscapeJs(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
             .Replace("'", "\\'", StringComparison.Ordinal)
             .Replace("\"", "\\\"", StringComparison.Ordinal);
}
