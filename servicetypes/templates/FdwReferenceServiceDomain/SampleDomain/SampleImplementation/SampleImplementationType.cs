using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SampleDomain.Abstractions;
using SampleDomain.SampleImplementation.Logging;

namespace SampleDomain.SampleImplementation;

/// <summary>
/// Declares the SAMPLE_OPTION implementation of SampleDomain to the platform.
/// </summary>
/// <remarks>
/// <para>
/// The <c>[Implementation]</c> attribute is the whole registration. A module initializer attaches it
/// at assembly load, so referencing this package is what makes SAMPLE_OPTION selectable — there is no
/// registry to edit and no switch to extend.
/// </para>
/// <para>
/// <c>Registration</c> runs before <c>Build()</c> and adds this implementation's configuration
/// provider. <c>Initialization</c> runs against the built container and registers that provider into
/// the domain provider under this option's Name — the domain provider holds one implementation
/// provider per <c>Implementation</c>, and this option is the only thing that knows which one it is.
/// </para>
/// <para>
/// Initialize is the right place only because the domain configuration provider is a Singleton: root's
/// one call reaches the instance every later resolution sees. A <b>scoped</b> registration — a runtime
/// service provider — is constructed again for every request scope, and a registration made into
/// root's instance at Initialize never reaches those. Such an option instead decorates the domain's
/// own <c>ServiceDescriptor</c> from Registration, so the registration runs inside whichever scope
/// constructs the instance. <c>EmailNotificationType</c> shows both halves side by side.
/// </para>
/// </remarks>
[Implementation(typeof(SampleDomainTypes), "SAMPLE_OPTION")]
public sealed class SampleImplementationType
    : SampleDomainTypeBase<IGenericService, ISampleDomainFactory<IGenericService, SampleImplementationConfiguration>, SampleImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="SampleImplementationType"/> class.</summary>
    public SampleImplementationType()
        : base(
            name: "SAMPLE_OPTION",
            sectionName: "SampleImplementation",
            displayName: "Sample implementation",
            description: "Replace this with what SAMPLE_OPTION actually is — the vendor, the protocol, the concrete thing this implementation talks to.",
            defaultContainerName: "SampleImplementation")
    {
        Registration((builder, loggerFactory) =>
        {
            // The store comes from the domain's registration, never from this option: an
            // implementation lives in the same store as the domain row that owns it.
            builder.Services.TryAddSingleton<ISampleImplementationConfigurationProvider>(sp =>
                new SampleImplementationConfigurationProvider(
                    sp.GetRequiredService<ILogger<SampleImplementationConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    SampleDomainTypes.ConfigurationConnection));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Initialization((host, loggerFactory) =>
        {
            var logger = loggerFactory?.CreateLogger<SampleImplementationType>()
                ?? NullLogger<SampleImplementationType>.Instance;

            var services = host.Services;
            var registered = services.GetRequiredService<ISampleDomainConfigurationProvider>()
                .Register(Name, services.GetRequiredService<ISampleImplementationConfigurationProvider>());
            if (registered.IsFailure)
                return registered.ToNewResult<IHost>();

            SampleImplementationLog.ConfigurationProviderRegistered(logger, Name);

            return GenericResult<IHost>.Success(host);
        });
    }
}
