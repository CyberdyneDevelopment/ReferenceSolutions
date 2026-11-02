using Fdw.Aegis.Abstractions;
using Fdw.Results;
using Fdw.Services.SecretManager;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Results;
using Fdw.Services.SecretManagers.Commands;
using Microsoft.Extensions.Logging;

namespace FdwReferenceSecretManagers.TestDouble;

/// <summary>
/// Test-owned <see cref="ISecretManager"/> that resolves secrets from environment variables.
/// </summary>
/// <remarks>
/// Deliberately minimal: the Aegis non-exposure suite only ever issues
/// <see cref="GetSecretManagerCommand"/>, so every other verb fails loud rather than pretending to
/// succeed. A stub that quietly returned success for an unimplemented verb would let a future test
/// pass while proving nothing.
/// </remarks>
public sealed class SyntheticSecretManager
    : SecretManagerServiceBase<GetSecretManagerCommand, SyntheticSecretManagerConfiguration, SyntheticSecretManager>
{
    private readonly string _prefix;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyntheticSecretManager"/> class.
    /// </summary>
    /// <param name="logger">The logger for this service.</param>
    /// <param name="configuration">The declared configuration, supplying the lookup prefix.</param>
    public SyntheticSecretManager(ILogger<SyntheticSecretManager> logger, SyntheticSecretManagerConfiguration configuration)
        : base(logger, configuration)
        => _prefix = configuration.Prefix;

    /// <inheritdoc />
    public override async Task<IGenericResult<object?>> Execute(ISecretManagerCommand managementCommand, CancellationToken cancellationToken = default)
    {
        if (managementCommand is not GetSecretManagerCommand get)
            return GenericResult<object?>.Failure(SecretManagerResultCodes.ByName("NoHandlerFound"));

        var typed = await Execute(get, cancellationToken).ConfigureAwait(false);
        return typed.IsSuccess
            ? GenericResult<object?>.Success(typed.Value)
            : typed.ToNewResult<object?>();
    }

    /// <inheritdoc />
    public override Task<IGenericResult<TResult>> Execute<TResult>(ISecretManagerCommand<TResult> managementCommand, CancellationToken cancellationToken = default)
    {
        if (managementCommand is not GetSecretManagerCommand get || get.SecretKey is null)
            return Task.FromResult(GenericResult<TResult>.Failure(SecretManagerResultCodes.ByName("NoHandlerFound")));

        var variable = _prefix + get.SecretKey;

        // Why fail rather than return an empty SecretValue: an absent secret is a missing required
        // input, and the non-exposure tests depend on that failure never reaching downstream.
        var value = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrEmpty(value))
            return Task.FromResult(GenericResult<TResult>.Failure(
                AegisResultCodes.ByName("SecretResolutionFailed"),
                ResultDetails.Create("Variable", variable)));

        return Task.FromResult(
            new SecretValue(get.SecretKey, value) is TResult secret
                ? GenericResult<TResult>.Success(secret)
                : GenericResult<TResult>.Failure(SecretManagerResultCodes.ByName("NoHandlerFound")));
    }

    /// <inheritdoc />
    public override async Task<IGenericResult> Execute(GetSecretManagerCommand command, CancellationToken cancellationToken = default)
        => await Execute((ISecretManagerCommand)command, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public override Task<IGenericResult<T>> Execute<T>(GetSecretManagerCommand command, CancellationToken cancellationToken = default)
        => Execute<T>((ISecretManagerCommand<T>)(object)command, cancellationToken);

    /// <inheritdoc />
    public override Task<IGenericResult> ExecuteBatch(IReadOnlyList<ISecretManagerCommand> commands, CancellationToken cancellationToken = default)
        => Task.FromResult<IGenericResult>(
            GenericResult.Failure(SecretManagerResultCodes.ByName("NoHandlerFound")));

    /// <inheritdoc />
    public override IGenericResult ValidateCommand(ISecretManagerCommand managementCommand)
        => managementCommand is GetSecretManagerCommand
            ? GenericResult.Success()
            : GenericResult.Failure(SecretManagerResultCodes.ByName("NoHandlerFound"));
}
