using System;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Hosts;
using Fdw.Services.Hosts.Abstractions;
using Fdw.Services.Identity;
using Fdw.Services.Identity.Abstractions;
using Fdw.Web.Http.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FdwReferenceHosts.OutboundIdentity.Configuration;
using FdwReferenceHosts.OutboundIdentity.Logging;

namespace FdwReferenceHosts.OutboundIdentity.Registration;

/// <summary>Supplies the token this host presents when it calls another service.</summary>
/// <remarks>
/// Register reads the row naming the <c>sec.Identity</c> this host authenticates as, and the audience and scopes it
/// asks for, and registers the <see cref="IAccessTokenProvider"/> the host's outbound HTTP clients attach. Initialization
/// resolves the row once, so a host with no row, or a row that does not name an identity and an audience, fails at
/// startup rather than on its first outbound call. Referencing this package is what a host does to authenticate as
/// its own identity; a host that forwards its caller's token instead registers a different provider.
/// <para>
/// There is no middleware here, which is why <see cref="HostTypeBase{TService, TConfiguration, TFactory}.PipelinePosition"/>
/// is left at its default.
/// </para>
/// </remarks>
[Implementation(typeof(HostTypes), "OutboundIdentity")]
public sealed class OutboundIdentityHostType
    : HostTypeBase<OutboundIdentityHostService, OutboundIdentityHostConfiguration, IOutboundIdentityHostFactory>
{
    // The implementation this option supplies to its domain -- the Implementation its domain row names.
    private const string HostImplementation = "Host";

    // The store the host features are read from: server settings, not platform configuration.
    private static string ConfigurationStore => ServerConnection.Name;

    /// <summary>Initializes a new instance of the <see cref="OutboundIdentityHostType"/> class.</summary>
    public OutboundIdentityHostType()
        : base(
            name: "OutboundIdentity",
            sectionName: "Host",
            displayName: "Outbound Identity",
            description: "The identity, audience and scopes this host authenticates with when it calls another service", defaultPathName: "hst", defaultContainerName: "HostOutboundIdentityConfiguration")
    {
        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IOutboundIdentityHostFactory), typeof(OutboundIdentityHostFactory), ServiceLifetime.Singleton));

            builder.Services.AddSingleton<IOutboundIdentityHostConfigurationProvider, OutboundIdentityHostConfigurationProvider>(sp => new OutboundIdentityHostConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<OutboundIdentityHostConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                ConfigurationStore, PathName, Container));

            // FDW supplies the OutboundIdentity domain provider, not its registration: the host names the store
            // the domain reads and the implementation it dispatches to.
            builder.Services.TryAddSingleton(sp =>
            {
                var domain = new OutboundIdentityConfigurationProvider(
                    sp.GetRequiredService<ILoggerFactory>().CreateLogger<OutboundIdentityConfigurationProvider>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ConfigurationStore, "hst", "OutboundIdentity");
                domain.Register(HostImplementation, sp.GetRequiredService<IOutboundIdentityHostConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IOutboundIdentityConfigurationProvider>(sp => sp.GetRequiredService<OutboundIdentityConfigurationProvider>());

            // The row, read and checked once: every outbound call on this host authenticates as the same identity.
            builder.Services.TryAddSingleton(sp => Resolve(
                Read(sp, Name),
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<OutboundIdentityHostType>()));

            // Scoped, as the identity service it calls is: one provider per outbound request scope.
            builder.Services.AddScoped<IAccessTokenProvider>(sp =>
            {
                var identity = sp.GetRequiredService<OutboundIdentityHostConfiguration>();
                return new ManagedIdentityAccessTokenProvider(
                    sp.GetRequiredService<IIdentityServiceProvider>(),
                    sp.GetRequiredService<IIdentityTokenCache>(),
                    identity.IdentityName,
                    new IdentityTokenRequest(identity.Audience, Scopes(identity)),
                    sp.GetService<ILogger<ManagedIdentityAccessTokenProvider>>());
            });

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Initialization((host, loggerFactory) =>
        {
            // Why resolve here when nothing is wired: resolving the row reads and checks it, and doing it at
            // startup makes a missing or incomplete row stop the host instead of surfacing on the first
            // outbound call.
            host.Services.GetRequiredService<OutboundIdentityHostConfiguration>();
            return GenericResult<IHost>.Success(host);
        });
    }

    /// <summary>Checks the configured row names an identity and an audience, and logs what this host authenticates as.</summary>
    /// <param name="row">The configured OutboundIdentityHost row.</param>
    /// <param name="logger">Logger for the resolved identity.</param>
    /// <returns>The row.</returns>
    internal static OutboundIdentityHostConfiguration Resolve(OutboundIdentityHostConfiguration row, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(row.IdentityName))
        {
            throw new InvalidOperationException(
                "The OutboundIdentity row names no IdentityName: the host cannot call another service without the "
                + "sec.Identity row it authenticates as.");
        }

        if (string.IsNullOrWhiteSpace(row.Audience))
        {
            throw new InvalidOperationException(
                "The OutboundIdentity row names no Audience: a token with no declared audience is one any peer "
                + "would have to accept on trust.");
        }

        OutboundIdentityHostLog.IdentityResolved(logger, row.IdentityName, row.Audience);
        return row;
    }

    // The row holds scopes as the identity rows do, space-separated; null when the identity derives its own.
    private static string[]? Scopes(OutboundIdentityHostConfiguration row)
        => row.Scopes?.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    // Why blocking: a registration factory cannot await, and the row decides which identity every outbound call uses.
    private static OutboundIdentityHostConfiguration Read(IServiceProvider services, string name)
    {
        var provider = services.GetRequiredService<IOutboundIdentityConfigurationProvider>();
#pragma warning disable VSTHRD002
        var result = provider.Get(name).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        if (result.IsFailure || result.Value is not OutboundIdentityHostConfiguration configuration)
        {
            throw new InvalidOperationException(
                $"No OutboundIdentity row named '{name}' is configured. Which identity this host authenticates as "
                + "is configuration rather than a default, so without the row it cannot call another service.");
        }

        return configuration;
    }
}
