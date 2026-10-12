using System;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Fdw.Services.Hosts;
using Fdw.Services.Hosts.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FdwReferenceHosts.ResponseStreaming.Configuration;
using FdwReferenceHosts.ResponseStreaming.Logging;

namespace FdwReferenceHosts.ResponseStreaming.Registration;

/// <summary>Supplies the limits every streaming endpoint on this host runs under.</summary>
/// <remarks>
/// Register reads the row and turns it into the <see cref="ResponseStreamingOptions"/> the streaming endpoints
/// consume; Initialization resolves them once, so a host with no row - or a row whose limits are not positive -
/// fails at startup rather than on its first export. That is the whole option: referencing this package is what
/// a host does to let its endpoints stream at all, because a streaming endpoint cannot be constructed without
/// the limits it supplies.
/// <para>
/// There is no middleware here, which is why <see cref="HostTypeBase{TService, TConfiguration, TFactory}.PipelinePosition"/>
/// is left at its default. ResponseBuffering is its sibling: that one decides whether a response is held
/// before it is written, this one the limits under which a response is written progressively.
/// </para>
/// </remarks>
[Implementation(typeof(HostTypes), "ResponseStreaming")]
public sealed class ResponseStreamingHostType
    : HostTypeBase<ResponseStreamingHostService, ResponseStreamingHostConfiguration, IResponseStreamingHostFactory>
{
    // The implementation this option supplies to its domain -- the Implementation its domain row names.
    private const string HostImplementation = "Host";

    // The store the host features are read from: server settings, not platform configuration.
    private static string ConfigurationStore => ServerConnection.Name;

    /// <summary>Initializes a new instance of the <see cref="ResponseStreamingHostType"/> class.</summary>
    public ResponseStreamingHostType()
        : base(
            name: "ResponseStreaming",
            sectionName: "Host",
            displayName: "Response Streaming",
            description: "The row ceiling and concurrent-stream limit every streaming endpoint on this host runs under", defaultPathName: "hst", defaultContainerName: "HostResponseStreamingConfiguration")
    {
        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IResponseStreamingHostFactory), typeof(ResponseStreamingHostFactory), ServiceLifetime.Singleton));

            builder.Services.AddSingleton<IResponseStreamingHostConfigurationProvider, ResponseStreamingHostConfigurationProvider>(sp => new ResponseStreamingHostConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<ResponseStreamingHostConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                ConfigurationStore, PathName, Container));

            // FDW supplies the ResponseStreaming domain provider, not its registration: the host names the store
            // the domain reads and the implementation it dispatches to.
            builder.Services.TryAddSingleton(sp =>
            {
                var domain = new ResponseStreamingConfigurationProvider(
                    sp.GetRequiredService<ILoggerFactory>().CreateLogger<ResponseStreamingConfigurationProvider>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ConfigurationStore, "hst", "ResponseStreaming");
                domain.Register(HostImplementation, sp.GetRequiredService<IResponseStreamingHostConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IResponseStreamingConfigurationProvider>(sp => sp.GetRequiredService<ResponseStreamingConfigurationProvider>());

            // What the streaming endpoints actually consume. Built from the row, once: the limits are the host's,
            // and every streaming endpoint on it is bound by the same ones.
            builder.Services.TryAddSingleton(sp => ToOptions(
                Read(sp, Name),
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<ResponseStreamingHostType>()));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Initialization((host, loggerFactory) =>
        {
            // Why resolve here when nothing is wired: resolving the options reads the row, and doing it at
            // startup makes a missing or invalid row stop the host instead of surfacing on the first export.
            host.Services.GetRequiredService<ResponseStreamingOptions>();
            return GenericResult<IHost>.Success(host);
        });
    }

    /// <summary>Builds the options the streaming endpoints consume from the configured row.</summary>
    /// <param name="row">The configured ResponseStreamingHost row.</param>
    /// <param name="logger">Logger for the resolved limits.</param>
    /// <returns>The options.</returns>
    /// <remarks>
    /// Validation is the options' own, not repeated here: a zero or negative limit throws from its
    /// constructor, naming the setting.
    /// </remarks>
    internal static ResponseStreamingOptions ToOptions(ResponseStreamingHostConfiguration row, ILogger logger)
    {
        var options = new ResponseStreamingOptions(row.MaxRows, row.MaxConcurrentStreams);
        ResponseStreamingHostLog.LimitsResolved(logger, options.MaxRows, options.MaxConcurrentStreams);
        return options;
    }

    // Why blocking: a registration factory cannot await, and the row decides every limit these options carry.
    private static ResponseStreamingHostConfiguration Read(IServiceProvider services, string name)
    {
        var provider = services.GetRequiredService<IResponseStreamingConfigurationProvider>();
#pragma warning disable VSTHRD002
        var result = provider.Get(name).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        if (result.IsFailure || result.Value is not ResponseStreamingHostConfiguration configuration)
        {
            throw new InvalidOperationException(
                $"No ResponseStreaming row named '{name}' is configured. The limits a streaming endpoint runs under "
                + "are configuration rather than a default, so without the row no endpoint can stream.");
        }

        return configuration;
    }
}
