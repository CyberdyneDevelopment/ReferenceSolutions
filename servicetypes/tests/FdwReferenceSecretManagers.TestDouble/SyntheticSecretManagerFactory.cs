using Fdw.Results;
using Fdw.Services;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Results;
using Microsoft.Extensions.Logging;

namespace FdwReferenceSecretManagers.TestDouble;

/// <summary>
/// Builds <see cref="SyntheticSecretManager"/> instances from a declared configuration.
/// </summary>
/// <remarks>
/// Takes the <see cref="SyntheticSecretManagerConfiguration"/> implementation record the domain provider
/// returns, its Name already stamped — the same shape every shipped backend factory uses. The base
/// rejects any other configuration type before this factory sees it.
/// </remarks>
public sealed class SyntheticSecretManagerFactory
    : ServiceFactoryBase<ISecretManager, SyntheticSecretManagerConfiguration>, ISyntheticSecretManagerFactory
{
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyntheticSecretManagerFactory"/> class.
    /// </summary>
    /// <param name="loggerFactory">Creates the logger the constructed manager writes through.</param>
    public SyntheticSecretManagerFactory(ILoggerFactory loggerFactory)
        => _loggerFactory = loggerFactory ?? throw new System.ArgumentNullException(nameof(loggerFactory));

    /// <inheritdoc />
    public override IGenericResult<ISecretManager> Create(SyntheticSecretManagerConfiguration configuration)
        => configuration is null
            ? GenericResult<ISecretManager>.Failure(SecretManagerResultCodes.ByName("InvalidCommandType"))
            : GenericResult<ISecretManager>.Success(
                new SyntheticSecretManager(_loggerFactory.CreateLogger<SyntheticSecretManager>(), configuration));
}
