using Fdw.Services.Abstractions;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Scheduling;
using Fdw.Services.Scheduling.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.ServiceTypes.Logging;
using Quartz;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceSchedulers.Quartz.Configuration;
using FdwReferenceSchedulers.Quartz.Services;

namespace FdwReferenceSchedulers.Quartz.Registration;

/// <summary>
/// Service type definition for the Quartz scheduler.
/// </summary>
/// <remarks>
/// Mirrors <c>MsSqlSecretManagerType</c>'s two-phase wiring: Registration wires the factory and the
/// typed-body configuration provider into DI; Initialization resolves both from the live container
/// and registers them with the domain's providers. This is the registration
/// <c>DefaultSchedulerType</c> never did — its Registration only ever wired
/// <c>ISchedulingFactory</c>, and never called <c>headerProvider.Register(Name, configProvider)</c>,
/// which is why <c>SchedulerConfiguration.Configuration</c> composed to null for every row
/// regardless of what was in the database.
/// </remarks>
[ExcludeFromCodeCoverage]
[Implementation(typeof(SchedulerTypes), "Quartz")]
public sealed class QuartzSchedulerType
    : SchedulerTypeBase<IFrameworkSchedulingService, QuartzSchedulerConfiguration, IQuartzSchedulingFactory>
{
    // The schedule history lives in the platform's own store, which the host declares; it is read through the
    // configuration gateway onto that store, never the request-facing Main gateway, which refuses requests over a
    // host-declared store (FDW-1068). Resolved when the factory is first built, which is after registration.
    private static IDataGateway ScheduleStoreGateway(IConfigurationGatewayProvider configurationGateways)
        => configurationGateways.Get(SchedulerTypes.ConfigurationConnection) is { IsSuccess: true, Value: { } gateway }
            ? gateway
            : throw new InvalidOperationException(
                $"No configuration gateway is available for connection '{SchedulerTypes.ConfigurationConnection}'. It must be declared in the host's configurationSchema.json.");

    /// <summary>Initializes a new instance of the <see cref="QuartzSchedulerType"/> class.</summary>
    public QuartzSchedulerType() : base(
        name: "Quartz",
        schedulingEngine: "Quartz.NET",
        jobExecutorType: typeof(ScheduledPipelineJob),
        triggerType: typeof(ICronTrigger),
        supportsRecurring: true,
        supportsDelayed: true,
        defaultContainerName: "QuartzSchedulerConfiguration")
    {
        Registration((builder, loggerFactory) =>
        {
            // No UseMicrosoftDependencyInjectionJobFactory call: it is obsolete in Quartz 3.15 —
            // the DI job factory is already the default, which is what lets ScheduledPipelineJob
            // take its dependencies through the constructor.
            builder.Services.AddQuartz();
            builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

            // Why the provider and not IDataGateway directly: FDW never registers IDataGateway. The
            // data-gateway option registers IDataGatewayFactory and IDataGatewayProvider only, and
            // says why -- Create() builds a fresh gateway per call, so a scoped registration would
            // capture one across a scope boundary. Asking the container for IDataGateway fails with
            // "No service for type ... has been registered", which this option did on every boot
            // because it had never been run. Services take providers; only providers take gateways.
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IQuartzSchedulingFactory),
                sp => new QuartzSchedulingFactory(
                sp.GetRequiredService<ILoggerFactory>(),
                sp.GetRequiredService<ISchedulerFactory>(),
                ScheduleStoreGateway(sp.GetRequiredService<IConfigurationGatewayProvider>())),
                ServiceLifetime.Singleton));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IQuartzSchedulingProvider),
                sp => new QuartzSchedulingProvider(sp.GetRequiredService<IQuartzSchedulingFactory>()),
                ServiceLifetime.Singleton));

            builder.Services.AddSingleton<IQuartzSchedulerConfigurationProvider, QuartzSchedulerConfigurationProvider>(sp => new QuartzSchedulerConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<QuartzSchedulerConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                SchedulerTypes.ConfigurationConnection, PathName, Container));
            builder.Services.AddHostedService(sp => new QuartzReconciliationService(
                sp.GetRequiredService<IScheduleConfigurationProvider>(),
                sp.GetRequiredService<IDomainConfigurationProvider<ISchedulerImplementationConfiguration>>(),
                sp.GetRequiredService<IQuartzSchedulingFactory>(),
                sp.GetRequiredService<IAuthenticationContextAccessor>(),
                TimeSpan.FromMinutes(1),
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<QuartzReconciliationService>()));

            // Decorate the domain's own AddScoped<ISchedulerServiceProvider> registration --
            // already in builder.Services by the time this runs, since SchedulerTypes' own
            // Registration body sets it up before running the option loop.
            var existing = builder.Services.Single(d => d.ServiceType == typeof(ISchedulerServiceProvider));
            builder.Services.Remove(existing);
            builder.Services.AddScoped<ISchedulerServiceProvider>(sp =>
            {
                var provider = (ISchedulerServiceProvider)existing.ImplementationFactory!(sp);

                var factoryResult = provider.Register(Name, () => sp.GetRequiredService<IQuartzSchedulingProvider>());
                if (!factoryResult.IsSuccess)
                {
                    ServiceTypeLog.OptionFactoryRegistrationFailed(
                        sp.GetService<ILoggerFactory>()?.CreateLogger<QuartzSchedulerType>() ?? NullLogger<QuartzSchedulerType>.Instance,
                        nameof(QuartzSchedulerType), Name, nameof(QuartzSchedulingFactory), factoryResult.CurrentMessage);
                }

                return provider;
            });

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // ISchedulerConfigurationProvider is Singleton, so root's Initialize call reaches the same
        // instance every later resolution sees -- unlike the domain SERVICE provider above, this
        // one is safe to populate once, here, unmodified.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var headerResult = services.GetRequiredService<ISchedulerConfigurationProvider>()
                .Register(Name, services.GetRequiredService<IQuartzSchedulerConfigurationProvider>());
            if (!headerResult.IsSuccess)
                return headerResult.ToNewResult<IHost>();

            // Why here (FDW-902): every fire hands its schedule to the one scheduled execution
            // handler, so a host running this engine without one -- or with two -- fails now, not at
            // the first fire.
            var handlerResult = ScheduledExecutionHandlerTypes.RequireSingleHandler(
                services, Name,
                loggerFactory?.CreateLogger<QuartzSchedulerType>() ?? NullLogger<QuartzSchedulerType>.Instance);
            if (handlerResult.IsFailure)
                return handlerResult.ToNewResult<IHost>();

            return GenericResult<IHost>.Success(host);
        });
    }
}
