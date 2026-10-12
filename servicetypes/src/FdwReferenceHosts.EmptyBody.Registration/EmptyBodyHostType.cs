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
using FdwReferenceHost.Configuration;
using FdwReferenceHost.Middleware;
using FdwReferenceHosts.EmptyBody.Configuration;
using Fdw.Services.Hosts.Abstractions;

namespace FdwReferenceHosts.EmptyBody.Registration;

/// <summary>Answers a bodyless POST/PUT/PATCH with 400 rather than 415.</summary>
/// <remarks>
/// Register reads the row and prepares what the middleware needs; Initialize puts it in the
/// pipeline. That is the whole option — referencing this package is what a host does to get this
/// behaviour, and a host that wants only this takes only this.
/// </remarks>
[Implementation(typeof(HostTypes), "EmptyBody")]
public sealed class EmptyBodyHostType
    : HostTypeBase<EmptyBodyHostService, EmptyBodyHostConfiguration, IEmptyBodyHostFactory>
{
    // The implementation this option supplies to its domain -- the Implementation its domain row names.
    private const string HostImplementation = "Host";

    // The store the host features are read from: server settings, not platform configuration.
    private static string ConfigurationStore => ServerConnection.Name;

    /// <summary>Gets where this middleware sits in the pipeline.</summary>
    /// <remarks>Why 90: it rejects a malformed request after the caller is identified and limited.</remarks>
    public override int PipelinePosition => 90;

    /// <summary>Initializes a new instance of the <see cref="EmptyBodyHostType"/> class.</summary>
    public EmptyBodyHostType()
        : base(
            name: "EmptyBody",
            sectionName: "Host",
            displayName: "Empty Body",
            description: "The routes on this host whose requests may arrive without a body", defaultPathName: "hst", defaultContainerName: "HostEmptyBodyConfiguration")
    {
        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IEmptyBodyHostFactory), typeof(EmptyBodyHostFactory), ServiceLifetime.Singleton));

            builder.Services.AddSingleton<IEmptyBodyHostConfigurationProvider, EmptyBodyHostConfigurationProvider>(sp => new EmptyBodyHostConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<EmptyBodyHostConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                ConfigurationStore, PathName, Container));

            // FDW supplies the EmptyBody domain provider, not its registration: the host names the store
            // the domain reads and the implementation it dispatches to.
            builder.Services.TryAddSingleton(sp =>
            {
                var domain = new EmptyBodyConfigurationProvider(
                    sp.GetRequiredService<ILoggerFactory>().CreateLogger<EmptyBodyConfigurationProvider>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ConfigurationStore, "hst", "EmptyBody");
                domain.Register(HostImplementation, sp.GetRequiredService<IEmptyBodyHostConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IEmptyBodyConfigurationProvider>(sp => sp.GetRequiredService<EmptyBodyConfigurationProvider>());

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Initialization((host, loggerFactory) =>
        {
            if (host is not IApplicationBuilder app)
            {
                return GenericResult<IHost>.Success(host);
            }

            var configuration = Read(host, Name);
            app.UseMiddleware<EmptyBodyBadRequestMiddleware>(
                new EmptyBodyOptions
                {
                    BodylessPaths = configuration.BodylessPaths.Select(b => b.Path).ToList(),
                    BodylessRoutes = configuration.BodylessRoutes
                        .Select(r => new BodylessRoute(r.Prefix, r.Suffix))
                        .ToList(),
                });

            return GenericResult<IHost>.Success(host);
        });
    }

    // Why blocking: a phase body cannot await, and the row decides what this middleware does.
    private static EmptyBodyHostConfiguration Read(IHost host, string name)
    {
        var provider = host.Services.GetRequiredService<IEmptyBodyConfigurationProvider>();
#pragma warning disable VSTHRD002
        var result = provider.Get(name).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        if (result.IsFailure || result.Value is not EmptyBodyHostConfiguration configuration)
        {
            throw new InvalidOperationException(
                $"No EmptyBody row named '{name}' is configured. What this middleware does is "
                + "configuration rather than a default.");
        }

        return configuration;
    }
}
