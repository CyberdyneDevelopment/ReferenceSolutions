using Fdw.Collections;
using Fdw.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Fdw.Services.Hosts;
using FdwReferenceHosts.WebMcp.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Hosts.Abstractions;
using Microsoft.Extensions.Logging;

namespace FdwReferenceHosts.WebMcp.Registration;

/// <summary>
/// Declares WebMCP as a host option, so a host that references this package gets its client key and
/// header from the server configuration tier instead of a GetSection bind in its own Program.cs.
/// </summary>
[Implementation(typeof(HostTypes), "WebMcp")]
public sealed class WebMcpHostType
    : HostTypeBase<WebMcpHostService, WebMcpHostConfiguration, IWebMcpHostFactory>
{
    // The implementation this option supplies to its domain -- the Implementation its domain row names.
    private const string HostImplementation = "Host";

    // The store the host features are read from: server settings, not platform configuration.
    private static string ConfigurationStore => ServerConnection.Name;

    /// <summary>Initializes a new instance of the <see cref="WebMcpHostType"/> class.</summary>
    public WebMcpHostType()
        : base(
            name: "WebMcp",
            sectionName: "Host",
            displayName: "WebMCP",
            description: "What this host serves over WebMCP: the client key and its header, and the address MCP tool calls are sent to", defaultPathName: "hst", defaultContainerName: "HostWebMcpConfiguration")
    {
        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IWebMcpHostFactory), typeof(WebMcpHostFactory), ServiceLifetime.Singleton));

            builder.Services.AddSingleton<IWebMcpHostConfigurationProvider, WebMcpHostConfigurationProvider>(sp => new WebMcpHostConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<WebMcpHostConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                ConfigurationStore, PathName, Container));

            // FDW supplies the WebMcp domain provider, not its registration: the host names the store
            // the domain reads and the implementation it dispatches to.
            builder.Services.TryAddSingleton(sp =>
            {
                var domain = new WebMcpConfigurationProvider(
                    sp.GetRequiredService<ILoggerFactory>().CreateLogger<WebMcpConfigurationProvider>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ConfigurationStore, "hst", "WebMcp");
                domain.Register(HostImplementation, sp.GetRequiredService<IWebMcpHostConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IWebMcpConfigurationProvider>(sp => sp.GetRequiredService<WebMcpConfigurationProvider>());

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }
}
