using System;
using System.Linq;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Hosts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FdwReferenceHosts.ForwardedHeaders.Configuration;
using Fdw.Services.Hosts.Abstractions;

namespace FdwReferenceHosts.ForwardedHeaders.Registration;

/// <summary>Trusts the forwarded headers a reverse proxy sets.</summary>
/// <remarks>
/// Register reads the row and prepares what the middleware needs; Initialize puts it in the
/// pipeline. That is the whole option — referencing this package is what a host does to get this
/// behaviour, and a host that wants only this takes only this.
/// </remarks>
[Implementation(typeof(HostTypes), "ForwardedHeaders")]
public sealed class ForwardedHeadersHostType
    : HostTypeBase<ForwardedHeadersHostService, ForwardedHeadersHostConfiguration, IForwardedHeadersHostFactory>
{
    // The implementation this option supplies to its domain -- the Implementation its domain row names.
    private const string HostImplementation = "Host";

    // The store the host features are read from: server settings, not platform configuration.
    private static string ConfigurationStore => ServerConnection.Name;

    /// <summary>Gets where this middleware sits in the pipeline.</summary>
    /// <remarks>Why 10: the scheme and caller address every later component reads must be corrected first.</remarks>
    public override int PipelinePosition => 10;

    /// <summary>Initializes a new instance of the <see cref="ForwardedHeadersHostType"/> class.</summary>
    public ForwardedHeadersHostType()
        : base(
            name: "ForwardedHeaders",
            sectionName: "Host",
            displayName: "Forwarded Headers",
            description: "Which proxy networks this host trusts X-Forwarded-* from", defaultPathName: "hst", defaultContainerName: "HostForwardedHeadersConfiguration")
    {
        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IForwardedHeadersHostFactory), typeof(ForwardedHeadersHostFactory), ServiceLifetime.Singleton));

            builder.Services.AddSingleton<IForwardedHeadersHostConfigurationProvider, ForwardedHeadersHostConfigurationProvider>(sp => new ForwardedHeadersHostConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<ForwardedHeadersHostConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                ConfigurationStore, PathName, Container));

            // FDW supplies the ForwardedHeaders domain provider, not its registration: the host names the store
            // the domain reads and the implementation it dispatches to.
            builder.Services.TryAddSingleton(sp =>
            {
                var domain = new ForwardedHeadersConfigurationProvider(
                    sp.GetRequiredService<ILoggerFactory>().CreateLogger<ForwardedHeadersConfigurationProvider>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ConfigurationStore, "hst", "ForwardedHeaders");
                domain.Register(HostImplementation, sp.GetRequiredService<IForwardedHeadersHostConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IForwardedHeadersConfigurationProvider>(sp => sp.GetRequiredService<ForwardedHeadersConfigurationProvider>());

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Initialization((host, loggerFactory) =>
        {
            if (host is not IApplicationBuilder app)
            {
                return GenericResult<IHost>.Success(host);
            }

            var configuration = Read(host, Name);
            var options = new ForwardedHeadersOptions
            {
                ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                                 | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
                                 | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost,
            };

            // No declared network means trust any upstream: the row says so rather than a comment.
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            foreach (var network in configuration.TrustedProxyNetworks.Select(n => n.Network))
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }

            app.UseForwardedHeaders(options);

            return GenericResult<IHost>.Success(host);
        });
    }

    // Why blocking: a phase body cannot await, and the row decides what this middleware does.
    private static ForwardedHeadersHostConfiguration Read(IHost host, string name)
    {
        var provider = host.Services.GetRequiredService<IForwardedHeadersConfigurationProvider>();
#pragma warning disable VSTHRD002
        var result = provider.Get(name).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        if (result.IsFailure || result.Value is not ForwardedHeadersHostConfiguration configuration)
        {
            throw new InvalidOperationException(
                $"No ForwardedHeaders row named '{name}' is configured. What this middleware does is "
                + "configuration rather than a default.");
        }

        return configuration;
    }
}
