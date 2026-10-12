using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using SampleDomain.Abstractions;

namespace SampleDomain;

/// <summary>Supplies the configured members of SampleDomain.</summary>
/// <remarks>
/// Every read, save and delete belongs to
/// <see cref="DomainConfigurationProviderBase{TImplementationConfiguration}"/>. This class supplies a
/// store, a schema and a table, and nothing else — there is no logic to put in one. Implementations
/// are registered into it by name, each by its own option.
/// </remarks>
public sealed class SampleDomainConfigurationProvider
    : DomainConfigurationProviderBase<ISampleDomainImplementationConfiguration>,
      ISampleDomainConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="SampleDomainConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's rows are read from and written to — passed by its registration.</param>
    public SampleDomainConfigurationProvider(
        ILogger<SampleDomainConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "SAMPLE_CATEGORY", "SAMPLE_CATEGORY")
    {
    }
}
