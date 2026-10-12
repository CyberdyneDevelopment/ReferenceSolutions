using System.Linq;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.ServiceTypes.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RootNamespace.ServiceName.Abstractions;

namespace RootNamespace.ServiceName.ImplName;

/// <summary>
/// Declares the ImplName implementation of the ServiceName domain.
/// </summary>
/// <remarks>
/// <para>
/// Two registrations join this option to its domain, and their lifetimes decide where each goes.
/// </para>
/// <para>
/// <b>The domain's service provider is scoped</b> — a fresh instance for every request scope. So this
/// option decorates the domain's own registration from <c>Registration</c>: it removes that
/// <c>ServiceDescriptor</c> and re-adds a factory that builds the previous instance and registers this
/// option's implementation provider into it. The registration then runs inside whichever scope
/// constructs the instance. Registering from <c>Initialize</c> instead reaches only root's instance, and
/// every request fails with "No factory registered for service option type 'ImplName'". The domain adds
/// its own registration before running its options, so it is there to decorate, and decorations
/// compose across however many options the domain has.
/// </para>
/// <para>
/// <b>The domain's configuration provider is a singleton</b>, so root's one <c>Initialize</c> call
/// reaches the instance every later resolution sees. This option registers its implementation
/// configuration provider into it there.
/// </para>
/// <para>
/// <c>IImplNameServiceNameProvider</c> and <c>IImplNameServiceNameConfigurationProvider</c> are this
/// implementation's own providers, added beside this file — see <c>EmailNotificationProvider</c> and
/// <c>EmailNotificationConfigurationProvider</c> for their shape. Rename <c>IServiceNameServiceProvider</c>
/// and <c>IServiceNameConfigurationProvider</c> to the interfaces your domain actually declares.
/// </para>
/// </remarks>
[Implementation(typeof(ServiceNameTypes), "ImplName")]
public sealed class ImplNameServiceNameType
    : ServiceNameTypeBase<IServiceNameService, IImplNameServiceNameFactory, ImplNameServiceNameConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImplNameServiceNameType"/> class.
    /// </summary>
    public ImplNameServiceNameType()
        : base(
            name: "ImplName",
            sectionName: "ImplName",
            displayName: "ImplName ServiceName",
            description: "ImplName implementation for ServiceName services")
    {
        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAddSingleton<IImplNameServiceNameFactory, ImplNameServiceNameFactory>();
            builder.Services.TryAddSingleton<IImplNameServiceNameProvider>(
                sp => new ImplNameServiceNameProvider(sp.GetRequiredService<IImplNameServiceNameFactory>()));
            builder.Services.TryAddSingleton<IImplNameServiceNameConfigurationProvider>(
                sp => new ImplNameServiceNameConfigurationProvider(
                    sp.GetRequiredService<ILogger<ImplNameServiceNameConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ServiceNameTypes.ConfigurationConnection));

            var existing = builder.Services.Single(d => d.ServiceType == typeof(IServiceNameServiceProvider));
            builder.Services.Remove(existing);
            builder.Services.AddScoped<IServiceNameServiceProvider>(sp =>
            {
                var provider = (IServiceNameServiceProvider)existing.ImplementationFactory!(sp);

                var registered = provider.Register(Name, () => sp.GetRequiredService<IImplNameServiceNameProvider>());
                if (!registered.IsSuccess)
                {
                    ServiceTypeLog.OptionFactoryRegistrationFailed(
                        sp.GetService<ILoggerFactory>()?.CreateLogger<ImplNameServiceNameType>() ?? NullLogger<ImplNameServiceNameType>.Instance,
                        nameof(ImplNameServiceNameType), Name, nameof(IImplNameServiceNameProvider), registered.CurrentMessage);
                }

                return provider;
            });

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var registered = services.GetRequiredService<IServiceNameConfigurationProvider>()
                .Register(Name, services.GetRequiredService<IImplNameServiceNameConfigurationProvider>());

            return registered.IsFailure
                ? registered.ToNewResult<IHost>()
                : GenericResult<IHost>.Success(host);
        });
    }
}
