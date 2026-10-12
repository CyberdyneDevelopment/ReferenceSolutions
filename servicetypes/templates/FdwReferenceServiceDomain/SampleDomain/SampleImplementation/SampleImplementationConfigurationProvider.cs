using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using SampleDomain.Abstractions;

namespace SampleDomain.SampleImplementation;

/// <summary>Supplies the SAMPLE_OPTION implementation's own configuration.</summary>
/// <remarks>
/// Reads <c>SAMPLE_CATEGORY.SampleImplementation</c>. Its rows are always reached through the domain
/// row that owns them: the caller passes the domain row's Id and the join happens inside the query.
/// Everything else belongs to
/// <see cref="ImplementationProviderBase{TConfiguration, TContract}"/>.
/// </remarks>
public sealed class SampleImplementationConfigurationProvider
    : ImplementationProviderBase<SampleImplementationConfiguration, ISampleDomainImplementationConfiguration>,
      ISampleImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="SampleImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The store this provider's rows live in — passed by its registration.</param>
    public SampleImplementationConfigurationProvider(
        ILogger<SampleImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "SAMPLE_CATEGORY", "SampleImplementation")
    {
    }
}
