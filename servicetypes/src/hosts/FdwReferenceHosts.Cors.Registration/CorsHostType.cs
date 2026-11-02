using System;
using System.Linq;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Hosts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FdwReferenceHosts.Cors.Configuration;
using Fdw.Services.Hosts.Abstractions;

namespace FdwReferenceHosts.Cors.Registration;

/// <summary>
/// Adds this host's cross-origin policy and puts it in the pipeline.
/// </summary>
/// <remarks>
/// Register reads the row and adds the policy; Initialize puts it in the pipeline. That is the
/// whole option — referencing this package is what a host does to get CORS, and a host that wants
/// only CORS takes only this.
/// </remarks>
[Implementation(typeof(HostTypes), "Cors")]
public sealed class CorsHostType
    : HostTypeBase<CorsHostService, CorsHostConfiguration, ICorsHostFactory>
{
    // The implementation this option supplies to its domain -- the Implementation its domain row names.
    private const string HostImplementation = "Host";

    // The store the host features are read from: server settings, not platform configuration.
    private static string ConfigurationStore => ServerConnection.Name;

    /// <summary>Gets where this middleware sits in the pipeline.</summary>
    /// <remarks>Why 40: a preflight is answered before authentication rejects it.</remarks>
    public override int PipelinePosition => 40;

    /// <summary>Initializes a new instance of the <see cref="CorsHostType"/> class.</summary>
    public CorsHostType()
        : base(
            name: "Cors",
            sectionName: "Host",
            displayName: "CORS",
            description: "The cross-origin policy this host applies, read from the server configuration tier", defaultPathName: "crs", defaultContainerName: "HostCorsConfiguration")
    {
        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(ICorsHostFactory), typeof(CorsHostFactory), ServiceLifetime.Singleton));

            builder.Services.AddSingleton<ICorsHostConfigurationProvider, CorsHostConfigurationProvider>(sp => new CorsHostConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<CorsHostConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                ConfigurationStore, PathName, Container));

            // FDW supplies the Cors domain provider, not its registration: the host names the
            // implementation it dispatches to.
            builder.Services.TryAddSingleton(sp =>
            {
                var domain = new CorsConfigurationProvider(
                    sp.GetRequiredService<ILoggerFactory>().CreateLogger<CorsConfigurationProvider>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ConfigurationStore, "crs", "Cors");
                domain.Register(HostImplementation, sp.GetRequiredService<ICorsHostConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<ICorsConfigurationProvider>(sp => sp.GetRequiredService<CorsConfigurationProvider>());

            // Why deferred: nothing reads a configuration row while the container is still being
            // built. CorsOptions is configured when it is resolved, which is after the container
            // exists, so the policy comes through the domain provider like every other read.
            builder.Services.AddCors();
            builder.Services.AddOptions<CorsOptions>().Configure<IServiceProvider>((options, sp) =>
            {
                var configuration = Read(sp, Name);
                if (!configuration.Enabled) return;

                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(configuration.Origins.Select(o => o.Origin).ToArray())
                          .WithMethods(configuration.Methods.Select(m => m.Method).ToArray())
                          .WithHeaders(configuration.Headers.Where(h => !h.IsExposed).Select(h => h.HeaderName).ToArray())
                          .WithExposedHeaders(configuration.Headers.Where(h => h.IsExposed).Select(h => h.HeaderName).ToArray())
                          .SetPreflightMaxAge(TimeSpan.FromSeconds(configuration.PreflightMaxAgeSeconds));
                    if (configuration.AllowCredentials)
                    {
                        policy.AllowCredentials();
                    }
                });
            });

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Initialization((host, loggerFactory) =>
        {
            if (host is IApplicationBuilder app)
            {
                app.UseCors();
            }

            return GenericResult<IHost>.Success(host);
        });
    }

    // Why blocking: configuring options is synchronous, and what this host allows cross-origin
    // is the row rather than a default.
    private static CorsHostConfiguration Read(IServiceProvider services, string name)
    {
        var provider = services.GetRequiredService<ICorsConfigurationProvider>();
#pragma warning disable VSTHRD002
        var result = provider.Get(name).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        if (result.IsFailure || result.Value is not CorsHostConfiguration configuration)
        {
            throw new InvalidOperationException(
                $"No Cors row named '{name}' is configured. Whether this host allows cross-origin "
                + "requests, and from where, is configuration rather than a default.");
        }

        return configuration;
    }
}
