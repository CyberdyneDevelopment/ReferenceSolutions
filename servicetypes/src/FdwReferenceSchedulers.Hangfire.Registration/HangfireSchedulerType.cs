using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Scheduling;
using Fdw.Services.Scheduling.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.ServiceTypes.Logging;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceSchedulers.Hangfire.Configuration;
using FdwReferenceSchedulers.Hangfire.Services;

namespace FdwReferenceSchedulers.Hangfire.Registration;

/// <summary>
/// Service type definition for the Hangfire scheduler.
/// </summary>
/// <remarks>
/// Mirrors <c>MsSqlSecretManagerType</c>'s two-phase wiring: Registration wires the factory and the
/// typed-body configuration provider into DI; Initialization resolves both from the live container
/// and registers them with the domain's providers. This is the registration
/// <c>DefaultSchedulerType</c> never did — see <c>QuartzSchedulerType</c>'s remarks for the full
/// explanation of the bug this replaces.
/// <para>
/// Storage is <c>Hangfire.SqlServer</c>, pointed at whichever connection
/// <see cref="HangfireSchedulerConfiguration.StorageConnectionName"/> names (OpsDb) and resolved
/// through <see cref="IConnectionConfigurationProvider"/> like any other connection. Hangfire owns
/// the schema and migrations inside that database — it does not go through DataGateway, which is a
/// requirement of the external library rather than a choice; see
/// <see cref="HangfireStorageConnectionFactory"/>.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[Implementation(typeof(SchedulerTypes), "Hangfire")]
public sealed class HangfireSchedulerType
    : SchedulerTypeBase<IFrameworkSchedulingService, HangfireSchedulerConfiguration, IHangfireSchedulingFactory>
{
    // The Implementation of the one data-gateway implementation FDW ships. Why the provider and not
    // IDataGateway: FDW never registers IDataGateway (the data-gateway option registers the factory and the
    // provider only, because Create() builds a fresh gateway per ask), so asking the container for it fails
    // with "No service for type ... has been registered" (FDW-1055). Services take providers; only providers
    // take gateways.
    private const string MainDataGatewayName = "Main";

    /// <summary>Initializes a new instance of the <see cref="HangfireSchedulerType"/> class.</summary>
    public HangfireSchedulerType() : base(
        name: "Hangfire",
        schedulingEngine: "Hangfire",
        jobExecutorType: typeof(HangfireJobDispatcher),
        triggerType: typeof(string),
        supportsRecurring: true,
        supportsDelayed: false,
        defaultContainerName: "HangfireSchedulerConfiguration")
    {
        Registration((builder, loggerFactory) =>
        {
            // Deferred (IServiceProvider, IGlobalConfiguration) overload: Hangfire needs its storage
            // configured here, but the connection naming it is a configuration row that cannot be
            // read until the container is built. This overload runs after that, which is what makes
            // a configured (rather than hardcoded) storage connection possible at all.
            builder.Services.AddHangfire((sp, config) =>
            {
                // Through the Scheduler domain, which reads each scheduler row and hands it to the
                // implementation it names; the Hangfire rows come back as HangfireSchedulerConfiguration,
                // which carries StorageConnectionName. Hangfire has one storage per process, so exactly
                // one Hangfire row is required rather than picking the first of several.
                var schedulerConfig = sp
                    .GetRequiredService<ISchedulerConfigurationProvider>()
                    .Find<HangfireSchedulerConfiguration>(static _ => true).GetAwaiter().GetResult();

                if (!schedulerConfig.IsSuccess || schedulerConfig.Value is not { Count: 1 } rows)
                    throw new InvalidOperationException(
                        "Exactly one Hangfire scheduler row is required to resolve Hangfire's storage connection.");

                var storageConnectionName = rows[0].StorageConnectionName;
                var connectionFactory = new HangfireStorageConnectionFactory(
                    sp.GetRequiredService<IConnectionConfigurationProvider>(),
                    sp.GetRequiredService<ISecretManagerProvider>(),
                    sp.GetRequiredService<ILoggerFactory>().CreateLogger<HangfireStorageConnectionFactory>());

                var factoryResult = connectionFactory.Create(storageConnectionName, CancellationToken.None).GetAwaiter().GetResult();
                if (!factoryResult.IsSuccess || factoryResult.Value is null)
                    throw new InvalidOperationException(factoryResult.CurrentMessage ?? $"Could not resolve Hangfire's storage connection '{storageConnectionName}'.");

                config.UseSqlServerStorage(factoryResult.Value);
            });

            // Without this Hangfire has storage and an API to enqueue into, but no worker pulling
            // jobs off and running them.
            builder.Services.AddHangfireServer();
            builder.Services.AddSingleton<IRecurringJobManager, RecurringJobManager>();

            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IHangfireSchedulingFactory),
                sp => new HangfireSchedulingFactory(
                sp.GetRequiredService<ILoggerFactory>(),
                sp.GetRequiredService<IRecurringJobManager>(),
                sp.GetRequiredService<IDataGatewayProvider>().ByName(MainDataGatewayName)),
                ServiceLifetime.Singleton));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IHangfireSchedulingProvider),
                sp => new HangfireSchedulingProvider(sp.GetRequiredService<IHangfireSchedulingFactory>()),
                ServiceLifetime.Singleton));

            // Read only through the Scheduler domain, which this provider is registered with in Initialize.
            builder.Services.TryAddSingleton<IHangfireSchedulerConfigurationProvider>(sp => new HangfireSchedulerConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<HangfireSchedulerConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                SchedulerTypes.ConfigurationConnection, PathName, Container));

            builder.Services.AddHostedService(sp => new HangfireReconciliationService(
                sp.GetRequiredService<ScheduleConfigurationProvider>(),
                sp.GetRequiredService<IDomainConfigurationProvider<ISchedulerImplementationConfiguration>>(),
                sp.GetRequiredService<IHangfireSchedulingFactory>(),
                sp.GetRequiredService<IAuthenticationContextAccessor>(),
                TimeSpan.FromMinutes(1),
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<HangfireReconciliationService>()));

            // Decorate the domain's own AddScoped<ISchedulerServiceProvider> registration --
            // already in builder.Services by the time this runs, since SchedulerTypes' own
            // Registration body sets it up before running the option loop.
            var existing = builder.Services.Single(d => d.ServiceType == typeof(ISchedulerServiceProvider));
            builder.Services.Remove(existing);
            builder.Services.AddScoped<ISchedulerServiceProvider>(sp =>
            {
                var provider = (ISchedulerServiceProvider)existing.ImplementationFactory!(sp);

                var factoryResult = provider.Register(Name, () => sp.GetRequiredService<IHangfireSchedulingProvider>());
                if (!factoryResult.IsSuccess)
                {
                    ServiceTypeLog.OptionFactoryRegistrationFailed(
                        sp.GetService<ILoggerFactory>()?.CreateLogger<HangfireSchedulerType>() ?? NullLogger<HangfireSchedulerType>.Instance,
                        nameof(HangfireSchedulerType), Name, nameof(HangfireSchedulingFactory), factoryResult.CurrentMessage);
                }

                return provider;
            });

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // ISchedulerConfigurationProvider is Singleton, so root's Initialize call reaches the same
        // instance every later resolution sees -- unlike the domain SERVICE provider above, this
        // one is safe to populate once, here, unmodified. Without it the header composes with no
        // typed body, and the failure surfaces much later as "No implementation configuration
        // provider registered for Implementation 'Hangfire'".
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var headerResult = services.GetRequiredService<ISchedulerConfigurationProvider>()
                .Register(Name, services.GetRequiredService<IHangfireSchedulerConfigurationProvider>());
            if (!headerResult.IsSuccess)
                return headerResult.ToNewResult<IHost>();

            // Why here (FDW-902): every fire hands its schedule to the one scheduled execution
            // handler, so a host running this engine without one -- or with two -- fails now, not at
            // the first fire.
            var handlerResult = ScheduledExecutionHandlerTypes.RequireSingleHandler(
                services, Name,
                loggerFactory?.CreateLogger<HangfireSchedulerType>() ?? NullLogger<HangfireSchedulerType>.Instance);
            if (handlerResult.IsFailure)
                return handlerResult.ToNewResult<IHost>();

            return GenericResult<IHost>.Success(host);
        });
    }
}
