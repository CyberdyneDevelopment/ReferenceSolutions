using Microsoft.Extensions.Logging.Abstractions;
﻿using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Telemetry;
using Fdw.Services.Telemetry.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FdwReferenceTelemetry.OpenTelemetry.Registration;

/// <summary>
/// The ASP.NET Core hosting option: supplies the request-pipeline settings a host reads while
/// building itself.
/// </summary>
/// <remarks>
/// Replaces the loose <c>GetSection(...).Get&lt;T&gt;()</c> calls reference-api makes outside any
/// ServiceType phase for CORS, response buffering and security headers, and the literals it uses for
/// route prefix, role claim type, trusted proxy networks and bodyless routes.
/// </remarks>
[Implementation(typeof(TelemetryTypes), "OpenTelemetry")]
public sealed class OpenTelemetryType
    : TelemetryTypeBase<ITelemetryService, OpenTelemetryConfiguration, IOpenTelemetryFactory>
{
    /// <summary>The domain whose row this option configures.</summary>
    /// <remarks>
    /// A domain provider's <c>Get(name)</c> takes the DOMAIN row's Name, and that row is named for
    /// the domain it configures — "Telemetry", carrying Implementation "OpenTelemetry". This
    /// option's own Name selects the implementation, not the row.
    /// </remarks>
    private const string DomainName = "Telemetry";

    /// <summary>Initializes a new instance of the <see cref="OpenTelemetryType"/> class.</summary>
    public OpenTelemetryType()
        : base(
            name: "OpenTelemetry",
            sectionName: "Hosting",
            displayName: "ASP.NET Core",
            description: "Request-pipeline configuration for an ASP.NET Core host")
    {
        // Everything this option does happens here, in one func. The Register phase takes the
        // builder and runs before Build, which is where AddOpenTelemetry has to go; and
        // PlatformServices.ConfigurationGateway.Register has already put IConfigurationGatewayProvider
        // into the container by the time any option registers, so the row is readable right here.
        Registration((builder, loggerFactory) =>
        {
            // No factory and no service provider, the same as Logging. Telemetry is configuration
            // applied to OpenTelemetry's builders, not an object something resolves and sends
            // commands to -- the telemetry domain has no service provider at all, and registering a
            // factory for one only made the domain look like a service domain it never was.

            builder.Services.AddSingleton<IOpenTelemetryConfigurationProvider, OpenTelemetryConfigurationProvider>(sp => new OpenTelemetryConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<OpenTelemetryConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                TelemetryTypes.ConfigurationConnection, "otel", "OpenTelemetryConfiguration"));

            // Read the row HERE, from a provider built out of the collection, and register this
            // implementation first so the read can dispatch. Reading in Initialize instead was tried
            // and does not work: AddOpenTelemetry's callbacks run during Build(), before any
            // Initialize, so the row has to be in hand while the builder is still open.
            // TelemetryTypes adds the domain provider before collecting its options, so it is already
            // in the collection here and this option only contributes its implementation. Nothing
            // resolves the Telemetry domain at runtime, so this registration does not need to outlive
            // the phase func.
            using var built = builder.Services.BuildServiceProvider();

            var domain = built.GetRequiredService<ITelemetryConfigurationProvider>();
            var registered = domain.Register(Name, built.GetRequiredService<IOpenTelemetryConfigurationProvider>());
            if (!registered.IsSuccess)
                return registered.ToNewResult<IHostApplicationBuilder>();

#pragma warning disable VSTHRD002
            var row = domain.Get(DomainName).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
            if (row.IsFailure || row.Value is null)
                return GenericResult<IHostApplicationBuilder>.Failure(
                    OpenTelemetryLog.TelemetryRowNotConfigured(
                        loggerFactory?.CreateLogger<OpenTelemetryType>()
                            ?? NullLogger<OpenTelemetryType>.Instance,
                        DomainName));

            var telemetry = row.Value;

            // Configured straight onto the builders, not through IDeferred*.Configure. The deferred
            // form runs its callback while the ServiceProvider is being built, and
            // AddAspNetCoreInstrumentation registers services of its own -- which throws
            // "Services cannot be configured after ServiceProvider has been created" at that point and
            // took the host down after it had otherwise started. Deferring bought nothing anyway: the
            // row was already read above, so there is nothing left to wait for.
            builder.Services.AddOpenTelemetry()
                .WithTracing(trace =>
                {
                    trace.ConfigureResource(resource => Describe(resource, telemetry, builder));
                    if (!telemetry.TracingEnabled) return;
                    trace.AddAspNetCoreInstrumentation(options => options.RecordException = true);
                    if (telemetry.ExportToConsole) trace.AddConsoleExporter();
                })
                .WithMetrics(meter =>
                {
                    meter.ConfigureResource(resource => Describe(resource, telemetry, builder));
                    if (!telemetry.MetricsEnabled) return;
                    meter.AddAspNetCoreInstrumentation();
                    if (telemetry.ExportToConsole) meter.AddConsoleExporter();
                });

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }


    private static void Describe(
        ResourceBuilder resource, ITelemetryImplementationConfiguration telemetry, IHostApplicationBuilder builder)
        => resource
            .AddService(telemetry.ServiceName)
            .AddAttributes([
                new("deployment.environment", builder.Environment.EnvironmentName),
                new("host.name", Environment.MachineName)
            ]);
}
