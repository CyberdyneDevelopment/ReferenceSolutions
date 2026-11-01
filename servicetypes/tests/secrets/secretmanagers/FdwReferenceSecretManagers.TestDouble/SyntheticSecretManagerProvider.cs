using Fdw.Results;
using Fdw.Services;
using Fdw.Services.SecretManagers.Abstractions;

namespace FdwReferenceSecretManagers.TestDouble;

/// <summary>Builds the synthetic secret manager used by tests.</summary>
public sealed class SyntheticSecretManagerProvider
    : ImplementationServiceProviderBase<ISecretManager, ISecretManagerImplementationConfiguration>
{
    private readonly ISyntheticSecretManagerFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="SyntheticSecretManagerProvider"/> class.</summary>
    /// <param name="factory">Builds the secret manager.</param>
    public SyntheticSecretManagerProvider(ISyntheticSecretManagerFactory factory) => _factory = factory;

    /// <inheritdoc />
    public override Task<IGenericResult<ISecretManager>> Create(
        ISecretManagerImplementationConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var created = _factory.Create(configuration);
        return Task.FromResult(created.IsSuccess && created.Value is not null
            ? GenericResult<ISecretManager>.Success(created.Value)
            : created.ToNewResult<ISecretManager>());
    }
}
