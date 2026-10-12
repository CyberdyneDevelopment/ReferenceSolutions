using System;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Hosts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FdwReferenceHost.Middleware;
using FdwReferenceHosts.SecurityHeaders.Configuration;
using Fdw.Services.Hosts.Abstractions;

namespace FdwReferenceHosts.SecurityHeaders.Registration;

/// <summary>Sets this host's security response headers.</summary>
/// <remarks>
/// Register reads the row and prepares what the middleware needs; Initialize puts it in the
/// pipeline. That is the whole option — referencing this package is what a host does to get this
/// behaviour, and a host that wants only this takes only this.
/// </remarks>
[Implementation(typeof(HostTypes), "SecurityHeaders")]
public sealed class SecurityHeadersHostType
    : HostTypeBase<SecurityHeadersHostService, SecurityHeadersHostConfiguration, ISecurityHeadersHostFactory>
{
    // The implementation this option supplies to its domain -- the Implementation its domain row names.
    private const string HostImplementation = "Host";

    // The store the host features are read from: server settings, not platform configuration.
    private static string ConfigurationStore => ServerConnection.Name;

    /// <summary>Gets where this middleware sits in the pipeline.</summary>
    /// <remarks>Why 30: headers are set on the way out, so the middleware goes on early.</remarks>
    public override int PipelinePosition => 30;

    /// <summary>Initializes a new instance of the <see cref="SecurityHeadersHostType"/> class.</summary>
    public SecurityHeadersHostType()
        : base(
            name: "SecurityHeaders",
            sectionName: "Host",
            displayName: "Security Headers",
            description: "The security headers this host sets on every response", defaultPathName: "hst", defaultContainerName: "HostSecurityHeadersConfiguration")
    {
        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(ISecurityHeadersHostFactory), typeof(SecurityHeadersHostFactory), ServiceLifetime.Singleton));

            builder.Services.AddSingleton<ISecurityHeadersHostConfigurationProvider, SecurityHeadersHostConfigurationProvider>(sp => new SecurityHeadersHostConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<SecurityHeadersHostConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                ConfigurationStore, PathName, Container));

            // FDW supplies the SecurityHeaders domain provider, not its registration: the host names the store
            // the domain reads and the implementation it dispatches to.
            builder.Services.TryAddSingleton(sp =>
            {
                var domain = new SecurityHeadersConfigurationProvider(
                    sp.GetRequiredService<ILoggerFactory>().CreateLogger<SecurityHeadersConfigurationProvider>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ConfigurationStore, "hst", "SecurityHeaders");
                domain.Register(HostImplementation, sp.GetRequiredService<ISecurityHeadersHostConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<ISecurityHeadersConfigurationProvider>(sp => sp.GetRequiredService<SecurityHeadersConfigurationProvider>());

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Initialization((host, loggerFactory) =>
        {
            if (host is not IApplicationBuilder app)
            {
                return GenericResult<IHost>.Success(host);
            }

            var configuration = Read(host, Name);
            app.UseMiddleware<SecurityHeadersMiddleware>(
                new FdwReferenceHost.Configuration.SecurityHeadersOptions
                {
                    AllowFraming = configuration.AllowFraming,
                    EnableDefaultCsp = configuration.EnableDefaultCsp,
                });

            return GenericResult<IHost>.Success(host);
        });
    }

    // Why blocking: a phase body cannot await, and the row decides what this middleware does.
    private static SecurityHeadersHostConfiguration Read(IHost host, string name)
    {
        var provider = host.Services.GetRequiredService<ISecurityHeadersConfigurationProvider>();
#pragma warning disable VSTHRD002
        var result = provider.Get(name).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        if (result.IsFailure || result.Value is not SecurityHeadersHostConfiguration configuration)
        {
            throw new InvalidOperationException(
                $"No SecurityHeaders row named '{name}' is configured. What this middleware does is "
                + "configuration rather than a default.");
        }

        return configuration;
    }
}
