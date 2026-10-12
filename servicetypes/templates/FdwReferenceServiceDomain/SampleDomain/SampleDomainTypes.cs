using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.ServiceTypes.Logging;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SampleDomain.Abstractions;

namespace SampleDomain;

/// <summary>
/// The SampleDomain service type collection.
/// </summary>
/// <remarks>
/// <para>
/// Configure, Register and Initialize are source-generated from this declaration, and each runs
/// once. Options attach themselves by carrying an <c>[Implementation]</c> attribute naming this
/// collection — the collection gathers what attached itself, it never looks for candidates.
/// <c>ServiceCategory</c> is what the generator names the PlatformServices accessor after.
/// </para>
/// <para>
/// The Register body below replaces the generated default, so it runs each option's own Register
/// itself. It adds the domain provider FIRST: every option registers something into, or around, the
/// domain's registration, and that registration has to exist by the time the option runs.
/// </para>
/// <para>
/// This template covers the configuration layer. A domain that also constructs runtime service
/// instances declares <c>ServiceInterface</c>/<c>ProviderType</c>/<c>ProviderInterface</c> on the
/// attribute below and adds its scoped service provider here, beside the configuration provider —
/// see <c>NotificationServiceTypes</c> and the README.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(SampleDomainTypeBase<IGenericService, ISampleDomainFactory<IGenericService, ISampleDomainImplementationConfiguration>, ISampleDomainImplementationConfiguration>),
    typeof(ISampleDomainType),
    typeof(SampleDomainTypes),
    ServiceCategory = "SAMPLE_CATEGORY")]
public partial class SampleDomainTypes : ServiceTypeCollectionBase<
    SampleDomainTypeBase<IGenericService, ISampleDomainFactory<IGenericService, ISampleDomainImplementationConfiguration>, ISampleDomainImplementationConfiguration>,
    ISampleDomainType>
{
    /// <summary>
    /// The connection this domain's configuration rows are read from and written to.
    /// </summary>
    /// <remarks>
    /// The host's <c>ConnectionRoles.Platform</c> declaration, so a host points the whole domain at its own
    /// configuration store. Every implementation lives in the same store as the domain row that owns it
    /// — the foreign key is declared on that row and cannot span connections — so this one value
    /// settles the domain and its implementations together.
    /// </remarks>
    public static string ConfigurationConnection => PlatformConnection.Name;

    static SampleDomainTypes()
    {
        Registration((builder, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<SampleDomainTypes>() ?? NullLogger<SampleDomainTypes>.Instance;

            // Registered against its own interface and depended on through it. Singleton: an
            // implementation registers itself into this instance once, from Initialize, and every
            // later resolution sees the same instance — see SampleImplementationType.
            builder.Services.TryAddSingleton<ISampleDomainConfigurationProvider>(sp =>
                new SampleDomainConfigurationProvider(
                    sp.GetRequiredService<ILogger<SampleDomainConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ConfigurationConnection));

            foreach (var option in Options)
            {
                var registered = option.Register(builder, loggerFactory);
                if (registered.IsFailure)
                    return registered;
            }

            ServiceTypeLog.DomainOptionsCollected(
                log, nameof(SampleDomainTypes), Options.Length,
                string.Join(", ", Options.Select(option => option.Name)));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
