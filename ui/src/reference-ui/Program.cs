using System;
using System.IdentityModel.Tokens.Jwt;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Fdw.ServiceTypes;
using Fdw.Services.Data;
using Fdw.Services.Connections.FileSystem;
using Fdw.Services.SecretManagers.EnvironmentVariable.Configuration;
using ReferenceSecretManagers.EnvironmentVariable.Services;
using Fdw.Services.HealthChecks.Monitoring;
using Fdw.Services.SessionState;
using Fdw.Services.SessionState.Clients;
using Fdw.Services.Authentication.Clients.Models;
using Fdw.Web.Clients.Abstractions.Registration;
using Fdw.Web.Http.Authentication;
using Fdw.Web.Http.Authentication.Blazor;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using Reference.Ui.Components;
using Reference.Ui.Logging;

namespace Reference.Ui;

/// <summary>
/// Entry point for the reference UI host.
/// </summary>
[ExcludeFromCodeCoverage]
public static class Program
{
    /// <summary>
    /// Builds and runs the host.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>0 when the host exits normally; 1 when startup or the run fails.</returns>
    public static async Task<int> Main(string[] args)
    {
        try
        {

            var builder = WebApplication.CreateBuilder(args);

            var assemblyVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetEntryAssembly()!.Location).ProductVersion ?? "unknown";
            builder.Host.UseSerilog((context, configuration) =>
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .Enrich.WithProperty("Version", assemblyVersion));

            // Why (FDW-578): DIRECT FileSystem-backed IConfigurationGateway for SecretManager config. FileSystem is
            // a folder of JSON files (config-data/**), NOT a database — this keeps the UI's "no server-core DB"
            // posture while reusing the exact same IConfigurationGateway mechanism every other app uses. Mirrors
            // Reference.Api's AddConfigurationGateway, FileSystem-flavoured. SecretManagerConfigurationProvider + Lazy<IConfigurationGateway> are wired by
            // the PlatformServices sweep below (their owning packages' [ServiceTypeOption] module initializers).
            builder.Services.AddConfigurationGateway<FileSystemConnectionFactory>(
                "configurationSchema.json",
                            // Why a constructor call rather than a type argument: AddConfigurationGateway no longer
                            // takes TSecretManager. The caller names the constructor, so the compiler checks that the
                            // secret manager can actually be built — and the logical name the schema declares is
                            // passed in rather than inferred.
                            (sp, name) => new EnvironmentVariableSecretManager(
                                sp.GetRequiredService<ILogger<EnvironmentVariableSecretManager>>(),
                                sp.GetRequiredService<EnvironmentVariableConfiguration>(),
                                name));

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddBlazorServerAuthentication();

            // ========================================================================
            // Authentication Services (Cookie-based with JWT tokens stored in cookie)
            // ========================================================================

            var rawApiUrl = builder.Configuration["ApiEndpoints:Api"];
            if (string.IsNullOrEmpty(rawApiUrl))
            {
                var startupLogger = LoggerFactory.Create(l => l.AddConsole()).CreateLogger("Reference.Ui.Startup");
                ProgramLog.ApiBaseUrlMissing(startupLogger);
                // Why 1 and not a bare return: as a top-level statement this exited 0, so a host with no
                // API base URL reported success and stopped. A missing required setting is a startup
                // failure and the exit code has to say so.
                return 1;
            }
            var apiBaseUrl = rawApiUrl.TrimEnd('/') + "/api/v1/";
            // Why: OpenIddict's /connect/token lives at the API root, not under /api/v1/ — auth calls
            // (login + refresh) target this base; domain API calls use apiBaseUrl.
            var authBaseUrl = rawApiUrl.TrimEnd('/') + "/";

            // Why: ApiEndpoints:Api is the ONE declared answer to "where is the API", and it is the only one a
            // deployment overrides. Every API client — including session state — resolves through ApiClients:BaseUrl
            // (ApiEndpointRegistration.ResolveEndpoint), so that value is DERIVED here rather than restated as a
            // second literal. Restating it meant a deployment that moved the API moved only the auth calls and left
            // every domain client pointing at the dev default.
            // Per-client overrides still win: resolution reads ApiClients:{ClientName}:BaseUrl before this flat key.
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["ApiClients:BaseUrl"] = apiBaseUrl,
            });
            var isDevelopment = builder.Environment.IsDevelopment();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = "Blazor";
                options.DefaultChallengeScheme = "Blazor";
            })
            .AddCookie("Blazor", options =>
            {
                options.LoginPath = "/login";
                options.AccessDeniedPath = "/access-denied";
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.HttpOnly = true;

                options.Events.OnValidatePrincipal = async context =>
                {
                    var tokens = context.Properties.GetTokens().ToList();
                    var accessToken = tokens.FirstOrDefault(t => t.Name == "access_token")?.Value;
                    var refreshToken = tokens.FirstOrDefault(t => t.Name == "refresh_token")?.Value;
                    var expiresAt = tokens.FirstOrDefault(t => t.Name == "expires_at")?.Value;

                    if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(expiresAt))
                    {
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync("Blazor");
                        return;
                    }

                    if (DateTimeOffset.TryParse(expiresAt, out var expiry) && expiry <= DateTimeOffset.UtcNow.AddMinutes(5))
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<CookieAuthenticationHandler>>();

                        if (string.IsNullOrEmpty(refreshToken))
                        {
                            // Why: no refresh token on the ticket means the refresh cycle cannot even attempt a
                            // call - this silently forced sign-out before, with no record of why.
                            AuthLog.TokenRefreshRequiresReauth(logger);
                            context.RejectPrincipal();
                            await context.HttpContext.SignOutAsync("Blazor");
                            return;
                        }

                        try
                        {
                            using var httpClient = new HttpClient { BaseAddress = new Uri(authBaseUrl) };
                            // Why: OpenIddict's /connect/token enforces HTTPS transport security; over the local
                            // (TLS-terminated) hop the scheme reads http, so forward the real https scheme that
                            // the UI itself was reached on. Without this the token endpoint rejects with 400.
                            httpClient.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");
                            // Why: OpenIddict refresh-token grant (RFC 6749 §6) — form-encoded POST to
                            // /connect/token, replacing the deleted JSON /auth/refresh endpoint.
                            var response = await httpClient.PostAsync("connect/token", new FormUrlEncodedContent(
                                new Dictionary<string, string>
                                {
                                    ["grant_type"] = "refresh_token",
                                    ["refresh_token"] = refreshToken,
                                    ["client_id"] = "reference-client",
                                }));

                            if (!response.IsSuccessStatusCode)
                            {
                                AuthLog.TokenRefreshFailed(logger, (int)response.StatusCode);
                                context.RejectPrincipal();
                                await context.HttpContext.SignOutAsync("Blazor");
                                return;
                            }

                            // Why: OpenIddict returns RFC 6749 snake_case (access_token, expires_in); TokenResponse
                            // uses PascalCase, so map with the snake_case naming policy.
                            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(
                                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
                            if (tokenResponse is null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                            {
                                // Why: a successful refresh response with no usable token also forces reauth -
                                // same outcome as the missing-refresh-token branch above, previously unlogged.
                                AuthLog.TokenRefreshRequiresReauth(logger);
                                context.RejectPrincipal();
                                await context.HttpContext.SignOutAsync("Blazor");
                                return;
                            }

                            var newExpiry = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
                            context.Properties.StoreTokens(
                            [
                                new AuthenticationToken { Name = "access_token", Value = tokenResponse.AccessToken },
                                new AuthenticationToken { Name = "refresh_token", Value = tokenResponse.RefreshToken },
                                new AuthenticationToken { Name = "expires_at", Value = newExpiry.ToString("o") }
                            ]);
                            context.ShouldRenew = true;

                            AuthLog.TokenRefreshSucceeded(logger);
                        }
                        catch (Exception ex)
                        {
                            AuthLog.TokenRefreshException(logger, ex);
                            context.RejectPrincipal();
                            await context.HttpContext.SignOutAsync("Blazor");
                        }
                    }
                };
            });
            builder.Services.AddAuthorizationCore();
            builder.Services.AddCascadingAuthenticationState();

            // ========================================================================
            // HTTP Client Configuration
            // ========================================================================

            // Auth HTTP client (used by login endpoint — no bearer handler)
            builder.Services.AddHttpClient("AuthApi", client =>
            {
                client.BaseAddress = new Uri(authBaseUrl);
                // Why: OpenIddict's /connect/token enforces HTTPS transport; over the local TLS-terminated hop
                // the scheme reads http, so forward the real https scheme the UI was reached on (else 400).
                client.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");
            });

            // ONE PlatformServices sweep replaces the per-domain Configure/Register ceremony: every
            // [ServiceTypeCollection] discovered by the generated module initializer participates
            // (ApiClientTypes, HealthMonitorTypes, SessionStateTypes, ...). Since the DataNode core extraction
            // (FDW-572), the UI's data-node trees come from Fdw.Data.Components' DataStoreProviderClientType
            // (swept via ApiClientTypes): a Clients-backed IDataStoreProvider over the API — no connection needed.
            // The UI's auth is the cookie scheme + password-grant HTTP calls above; it never runs a local auth service.
            // FDW-578 exception: this app now references ONE gateway-backed domain — Fdw.Services.Data (+ its
            // DataGatewayTypes / SecretManagerTypes) — so the sweep ALSO wires Lazy<IConfigurationGateway> and the
            // SecretManagerConfigurationProvider for the DIRECT FileSystem-backed config gateway added above. That
            // gateway reads a folder of JSON files (config-data/**), not a database, so the UI's "no server-core DB"
            // posture holds; Multitenancy and the OpenIddict auth server remain structurally unreferenced.
            // Why each phase is checked: they return a result rather than throwing, because ending the
            // process is the host's call and not the framework's. A discarded result means this host
            // boots past a domain that failed to configure or register — the exact silent start the
            // result exists to prevent.
            var configured = PlatformServices.Configure(builder);
            if (configured.IsFailure)
            {
                Log.Fatal("PlatformServices.Configure failed: {Reason}", configured.CurrentMessage);
                return 1;
            }

            var registered = PlatformServices.Register(builder);
            if (registered.IsFailure)
            {
                Log.Fatal("PlatformServices.Register failed: {Reason}", registered.CurrentMessage);
                return 1;
            }

            // Why: health monitoring is a full service domain (HealthMonitorTypes). This host's HealthMonitors
            // appsettings row (ServiceOptionType "HttpClient") selects the HTTP-proxy implementation that queries
            // the API's health endpoints; consumers resolve it through IHealthMonitorProvider — no direct
            // IHealthMonitorService registration exists (this replaces the old ApiClientTypes HealthMonitorClient option).
            builder.Services.AddHttpContextAccessor();

            var app = builder.Build();

            var initialized = PlatformServices.Initialize(app);
            if (initialized.IsFailure)
            {
                Log.Fatal("PlatformServices.Initialize failed: {Reason}", initialized.CurrentMessage);
                return 1;
            }

            // Configure pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            // Why: Emits one structured log per HTTP request (method, path, status, elapsed).
            // Needed to make the journal useful for post-deploy sanity and user-session tracing.
            app.UseSerilogRequestLogging();

            // .NET 10: MapStaticAssets replaces UseStaticFiles for Blazor apps
            app.MapStaticAssets();

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                // Why: behind nginx -> Caddy the upstream is non-loopback, so the default
                // KnownNetworks/KnownProxies filter drops X-Forwarded-Proto and Request.Scheme
                // stays http. Clear both to trust the preview proxy chain (preview env only).
                // .NET 10: KnownNetworks is deprecated (ASPDEPR005) in favour of KnownIPNetworks.
                KnownIPNetworks = { },
                KnownProxies = { }
            });

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                // Why: derived from the pages each PageType declares, so this cannot name an assembly holding none of
                // them. Distinct() is still needed — the reorg consolidated 19 *.UI.Pages into one assembly, so many
                // page groups yield the same assembly, and Blazor's component discovery throws "Assembly already
                // defined" on duplicates (same as the Router in Routes.razor).
                .AddAdditionalAssemblies(Fdw.UI.Navigation.PageTypes.All()
                    .SelectMany(p => p.PageAssemblies).Distinct().ToArray())
                .AddInteractiveServerRenderMode();

            // ========================================================================
            // Auth Endpoints — cookie-based login/logout
            // ========================================================================

            app.MapPost("/auth/login", async (HttpContext context, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory) =>
            {
                // Why a named category and not ILogger<Program>: Program is static and cannot be a type
                // argument, and "Auth" says what these lines are about better than the entry point would.
                var logger = loggerFactory.CreateLogger("Reference.Ui.Auth");
                var form = await context.Request.ReadFormAsync();
                var username = form["username"].ToString();
                var password = form["password"].ToString();
                var tenant = form["tenant"].ToString();
                var returnUrl = form["returnUrl"].ToString();

                if (string.IsNullOrEmpty(returnUrl))
                    returnUrl = "/";

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    context.Response.Redirect($"/login?error=invalid-credentials&returnUrl={Uri.EscapeDataString(returnUrl)}");
                    return;
                }

                try
                {
                    var httpClient = httpClientFactory.CreateClient("AuthApi");
                    // Why: OpenIddict password grant (form-encoded POST to /connect/token), replacing the deleted
                    // JSON /auth/token endpoint. tenant is forwarded so multi-tenant users get a tenant-scoped token.
                    var loginForm = new Dictionary<string, string>
                    {
                        ["grant_type"] = "password",
                        ["username"] = username,
                        ["password"] = password,
                        ["client_id"] = "reference-client",
                        ["scope"] = "fdw.api offline_access",
                    };
                    if (!string.IsNullOrWhiteSpace(tenant))
                        loginForm["tenant"] = tenant;

                    var response = await httpClient.PostAsync("connect/token", new FormUrlEncodedContent(loginForm));

                    if (!response.IsSuccessStatusCode)
                    {
                        AuthLog.LoginFailed(logger, username, (int)response.StatusCode);
                        context.Response.Redirect($"/login?error=invalid-credentials&returnUrl={Uri.EscapeDataString(returnUrl)}");
                        return;
                    }

                    // Why: OpenIddict returns RFC 6749 snake_case; map onto PascalCase TokenResponse.
                    var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
                    if (tokenResponse is null)
                    {
                        // Why: response.IsSuccessStatusCode passed but the body deserialized to nothing (e.g. a
                        // 204 with no content) - server-side auth may not be configured. Previously unlogged.
                        AuthLog.LoginEmptyResponse(logger, username, (int)response.StatusCode);
                        context.Response.Redirect($"/login?error=invalid-credentials&returnUrl={Uri.EscapeDataString(returnUrl)}");
                        return;
                    }

                    if (string.IsNullOrEmpty(tokenResponse.AccessToken))
                    {
                        // Why: body deserialized but the access token field was missing/empty - a response
                        // shape the client doesn't recognize. Previously unlogged.
                        AuthLog.LoginInvalidResponse(logger, username);
                        context.Response.Redirect($"/login?error=invalid-credentials&returnUrl={Uri.EscapeDataString(returnUrl)}");
                        return;
                    }

                    // Parse JWT to build ClaimsPrincipal
                    var handler = new JwtSecurityTokenHandler();
                    var jwt = handler.ReadJwtToken(tokenResponse.AccessToken);
                    var identity = new ClaimsIdentity(jwt.Claims, "jwt", "name", "role");
                    var principal = new ClaimsPrincipal(identity);

                    // Store tokens in the auth cookie
                    var expiry = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = expiry.AddDays(7) // Cookie outlives token; OnValidatePrincipal refreshes
                    };
                    authProperties.StoreTokens(
                    [
                        new AuthenticationToken { Name = "access_token", Value = tokenResponse.AccessToken },
                        new AuthenticationToken { Name = "refresh_token", Value = tokenResponse.RefreshToken },
                        new AuthenticationToken { Name = "expires_at", Value = expiry.ToString("o") }
                    ]);

                    await context.SignInAsync("Blazor", principal, authProperties);

                    AuthLog.LoginSucceeded(logger, username);
                    context.Response.Redirect(returnUrl);
                }
                catch (Exception ex)
                {
                    AuthLog.LoginException(logger, ex, username);
                    context.Response.Redirect($"/login?error=server-error&returnUrl={Uri.EscapeDataString(returnUrl)}");
                }
            }).DisableAntiforgery();

            app.MapGet("/auth/logout", async (HttpContext context, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory) =>
            {
                // Why a named category and not ILogger<Program>: Program is static and cannot be a type
                // argument, and "Auth" says what these lines are about better than the entry point would.
                var logger = loggerFactory.CreateLogger("Reference.Ui.Auth");
                // Best-effort server logout
                try
                {
                    var accessToken = await context.GetTokenAsync("access_token");
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        var httpClient = httpClientFactory.CreateClient("AuthApi");
                        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                        await httpClient.PostAsync("auth/logout", null);
                    }
                }
                catch (Exception ex)
                {
                    AuthLog.LogoutFailed(logger, ex);
                }

                await context.SignOutAsync("Blazor");
                context.Response.Redirect("/login");
            });

            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            return 1;
        }
    }
}

