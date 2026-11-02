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
using FdwReferenceHost.Middleware;
using FdwReferenceHosts.ResponseBuffering.Configuration;
using Fdw.Services.Hosts.Abstractions;

namespace FdwReferenceHosts.ResponseBuffering.Registration;

/// <summary>Buffers responses so a failed write can still be replaced.</summary>
/// <remarks>
/// Register reads the row and prepares what the middleware needs; Initialize puts it in the
/// pipeline. That is the whole option — referencing this package is what a host does to get this
/// behaviour, and a host that wants only this takes only this.
/// </remarks>
[Implementation(typeof(HostTypes), "ResponseBuffering")]
public sealed class ResponseBufferingHostType
    : HostTypeBase<ResponseBufferingHostService, ResponseBufferingHostConfiguration, IResponseBufferingHostFactory>
{
    // The implementation this option supplies to its domain -- the Implementation its domain row names.
    private const string HostImplementation = "Host";

    // The store the host features are read from: server settings, not platform configuration.
    private static string ConfigurationStore => ServerConnection.Name;

    /// <summary>Gets where this middleware sits in the pipeline.</summary>
    /// <remarks>Why 20: it has to wrap the response before anything writes to it.</remarks>
    public override int PipelinePosition => 20;

    /// <summary>Initializes a new instance of the <see cref="ResponseBufferingHostType"/> class.</summary>
    public ResponseBufferingHostType()
        : base(
            name: "ResponseBuffering",
            sectionName: "Host",
            displayName: "Response Buffering",
            description: "Whether this host buffers responses, and which paths bypass it", defaultPathName: "hst", defaultContainerName: "HostResponseBufferingConfiguration")
    {
        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IResponseBufferingHostFactory), typeof(ResponseBufferingHostFactory), ServiceLifetime.Singleton));

            builder.Services.AddSingleton<IResponseBufferingHostConfigurationProvider, ResponseBufferingHostConfigurationProvider>(sp => new ResponseBufferingHostConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<ResponseBufferingHostConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                ConfigurationStore, PathName, Container));

            // FDW supplies the ResponseBuffering domain provider, not its registration: the host names the store
            // the domain reads and the implementation it dispatches to.
            builder.Services.TryAddSingleton(sp =>
            {
                var domain = new ResponseBufferingConfigurationProvider(
                    sp.GetRequiredService<ILoggerFactory>().CreateLogger<ResponseBufferingConfigurationProvider>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ConfigurationStore, "hst", "ResponseBuffering");
                domain.Register(HostImplementation, sp.GetRequiredService<IResponseBufferingHostConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IResponseBufferingConfigurationProvider>(sp => sp.GetRequiredService<ResponseBufferingConfigurationProvider>());

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Initialization((host, loggerFactory) =>
        {
            if (host is not IApplicationBuilder app)
            {
                return GenericResult<IHost>.Success(host);
            }

            var configuration = Read(host, Name);
            if (!configuration.Enabled)
            {
                return GenericResult<IHost>.Success(host);
            }

            app.UseMiddleware<ResponseBufferingMiddleware>(
                new FdwReferenceHost.Configuration.ResponseBufferingOptions
                {
                    Enabled = configuration.Enabled,
                    MaxBufferBytes = configuration.MaxBufferBytes,
                    ExcludePathPrefixes = configuration.ExcludePathPrefixes.Select(e => e.PathPrefix).ToList(),
                });

            return GenericResult<IHost>.Success(host);
        });
    }

    // Why blocking: a phase body cannot await, and the row decides what this middleware does.
    private static ResponseBufferingHostConfiguration Read(IHost host, string name)
    {
        var provider = host.Services.GetRequiredService<IResponseBufferingConfigurationProvider>();
#pragma warning disable VSTHRD002
        var result = provider.Get(name).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        if (result.IsFailure || result.Value is not ResponseBufferingHostConfiguration configuration)
        {
            throw new InvalidOperationException(
                $"No ResponseBuffering row named '{name}' is configured. What this middleware does is "
                + "configuration rather than a default.");
        }

        return configuration;
    }
}
