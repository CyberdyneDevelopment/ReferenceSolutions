using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.ServiceNamePlural;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace FdwReferenceServiceNamePlural.ImplName;

/// <summary>
/// Service-type registration for the ImplName ServiceName.
/// </summary>
// Why: this type — and this package — is the REGISTRATION, separate from the aggregation it
// registers. Referencing this enlists the ServiceNamePlural domain; referencing only the aggregation
// package composes the service and enlists nothing. That choice is the reason for the split.
//
// Why the namespace matches the aggregation's: only the owning ASSEMBLY differs. Keeping the
// namespace means the fully-qualified name is unchanged, so this option's FNV-1a Id — derived from
// the FQN — does not move, and no persisted configuration has to be rewritten.
//
// TODO: close ServiceNameTypeBase on YOUR domain's real generic arguments and constructor. Most
// domains use <TService, TFactory, TConfiguration>, but check — the arity and the constructor
// parameters differ per domain, and the compiler is the only reliable oracle.
[Implementation(typeof(ServiceNameTypes), "ImplName")]
public sealed class ImplNameServiceNameType
    : ServiceNameTypeBase<ImplNameServiceName, IImplNameServiceNameFactory, ImplNameServiceNameConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImplNameServiceNameType"/> class.
    /// </summary>
    // Why the phases are set here: an option's Register and Initialize bodies are delegates handed to
    // Registration(...) and Initialization(...) in its constructor. There is no method to override.
    public ImplNameServiceNameType()
        : base(
            name: "ImplName",
            displayName: "ImplName ServiceName",
            description: "Reference ImplName implementation of the ServiceName domain.")
    {
        // Register runs while the container is still being built, so it can only add to it.
        Registration((builder, loggerFactory) =>
        {
            // Why: registered against the INTERFACE so the option never binds to the concrete
            // aggregation. DI supplies the constructor dependencies.
            builder.Services.TryAddSingleton<IImplNameServiceNameFactory, ImplNameServiceNameFactory>();

            // TODO: register this option into its domain. Two registrations, split by lifetime.
            //
            // 1. The domain's SERVICE provider is scoped: a fresh instance is built for every request
            //    scope. Decorate the domain's own registration here, so this option registers its
            //    implementation provider inside whichever scope builds the instance. The domain adds
            //    that registration before it runs its options, so it already exists:
            //
            //      var existing = builder.Services.Single(d => d.ServiceType == typeof(<domain provider interface>));
            //      builder.Services.Remove(existing);
            //      builder.Services.AddScoped<<domain provider interface>>(sp =>
            //      {
            //          var provider = (<domain provider interface>)existing.ImplementationFactory!(sp);
            //          provider.Register(Name, () => sp.GetRequiredService<IImplNameServiceNameProvider>());
            //          return provider;
            //      });
            //
            //    Registering into it from Initialize instead reaches only root's instance, and every
            //    request fails with "No factory registered for service option type 'ImplName'".
            //
            // 2. The domain's CONFIGURATION provider is a singleton, so register this option's
            //    implementation configuration provider into it from Initialization, below.
            //
            // Each domain names its own provider interfaces (INotificationServiceProvider,
            // IHealthMonitorProvider, ...). EmailNotificationType in FdwReferenceNotifications.Email.Registration
            // is both halves, working.
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // Initialize runs against the built container.
        Initialization((host, loggerFactory) =>
        {
            // TODO: host.Services.GetRequiredService<<domain configuration provider interface>>()
            //           .Register(Name, host.Services.GetRequiredService<IImplNameServiceNameConfigurationProvider>());
            return GenericResult<IHost>.Success(host);
        });
    }
}
