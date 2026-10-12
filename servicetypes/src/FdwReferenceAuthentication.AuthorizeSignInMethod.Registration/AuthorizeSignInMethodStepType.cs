using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Abstractions;
using Fdw.Claims.Abstractions;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Flow;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceAuthentication.AuthorizeSignInMethod.Registration;

/// <summary>
/// Refuses issuance to a principal who does not hold the permission this flow's settings name.
/// </summary>
/// <remarks>
/// <para>
/// What lets an operator decide, per sign-in method, who may use it. A flow without this step admits
/// every active user; a flow with it admits only principals whose baked permissions include the one its
/// row names; a flow switched off admits nobody. The permission comes from the roles the principal
/// holds, so allowing one more person is a role grant, not a configuration change.
/// </para>
/// <para>
/// It runs after BakePermissions, because it reads what that step baked — which is why it requires
/// <c>Claims</c>, and the flow provider refuses a flow that orders it earlier. It only ever refuses:
/// permitting is the account-eligibility step's decision, and a refusal here stands against it (the
/// runner never lets a later decision overrule an earlier refusal).
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[Implementation(typeof(AuthenticationStepTypes), "AuthorizeSignInMethod")]
public sealed class AuthorizeSignInMethodStepType
    : AuthenticationStepTypeBase<IGenericService, IServiceFactory<IGenericService, IServiceConfiguration>>,
      IAdministrablePermissionGate
{
    private ILogger _logger = NullLogger<AuthorizeSignInMethodStepType>.Instance;

    /// <summary>Initializes a new instance of the <see cref="AuthorizeSignInMethodStepType"/> class.</summary>
    public AuthorizeSignInMethodStepType()
        : base("AuthorizeSignInMethod",
               "AuthenticationSteps",
               "Authorize Sign-In Method",
               "Refuses issuance to a principal who lacks the permission this flow requires")
    {
        Initialization((host, loggerFactory) =>
        {
            _logger = loggerFactory?.CreateLogger<AuthorizeSignInMethodStepType>()
                ?? NullLogger<AuthorizeSignInMethodStepType>.Instance;

            return GenericResult<IHost>.Success(host);
        });
    }

    /// <inheritdoc />
    public IReadOnlyList<IContextElement> Requires => [ContextElements.Principal, ContextElements.Claims];

    /// <inheritdoc />
    public IReadOnlyList<IContextElement> Contributes => [ContextElements.Decision];

    /// <inheritdoc />
    /// <remarks>Deciding is not proving. Nothing about the caller is established here.</remarks>
    public IReadOnlyList<string> AuthenticationMethods => [];

    /// <inheritdoc />
    public IGenericResult ValidateConfiguration(string? configuration)
        => RequiredPermission(configuration) is { IsFailure: true } refused ? refused : GenericResult.Success();

    /// <inheritdoc />
    public IGenericResult<string> RequiredPermission(string? configuration)
    {
        if (configuration is null)
            return GenericResult<string>.Failure(AuthorizeSignInMethodLog.PermissionMissing(_logger));

        AuthorizeSignInMethodStepConfiguration? settings;
        try
        {
            settings = JsonSerializer.Deserialize<AuthorizeSignInMethodStepConfiguration>(configuration);
        }
        catch (JsonException ex)
        {
            return GenericResult<string>.Failure(AuthorizeSignInMethodLog.ConfigurationNotJson(_logger, ex.Message));
        }

        return settings?.RequiredPermission is { Length: > 0 } permission && !string.IsNullOrWhiteSpace(permission)
            ? GenericResult<string>.Success(permission)
            : GenericResult<string>.Failure(AuthorizeSignInMethodLog.PermissionMissing(_logger));
    }

    /// <inheritdoc />
    /// <remarks>Written through the same record <see cref="RequiredPermission"/> reads, so the two cannot drift apart.</remarks>
    public string ConfigurationRequiring(string permission)
        => JsonSerializer.Serialize(new AuthorizeSignInMethodStepConfiguration { RequiredPermission = permission });

    /// <inheritdoc />
    /// <remarks>
    /// This step reads its flow's settings, so the runner always calls the other overload. Reached
    /// without them, it refuses rather than admit on settings it never saw.
    /// </remarks>
    public Task<IGenericResult<StepOutcome>> Execute(
        AuthenticationContext context, ITenantProvider tenants, CancellationToken cancellationToken = default)
        => Execute(context, configuration: null, tenants, cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<StepOutcome>> Execute(
        AuthenticationContext context, string? configuration, ITenantProvider tenants, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var required = RequiredPermission(configuration);
        if (required.IsFailure)
            return Task.FromResult(required.ToNewResult<StepOutcome>());

        var principal = context.Principal!;
        var permission = required.Value!;

        // Only what this platform stated counts: a permission an outside provider asserted is advisory,
        // exactly as it is when the token is minted.
        var held = context.Claims.Claims.Any(c =>
            string.Equals(c.Type, ClaimDefinitions.perm.Name, StringComparison.Ordinal)
            && c.Source == ClaimSources.Local
            && string.Equals(c.Value, permission, StringComparison.OrdinalIgnoreCase));

        if (held)
        {
            AuthorizeSignInMethodLog.Admitted(_logger, principal.Id, permission);

            // Admitting contributes nothing: whether the account may hold a token at all is the
            // eligibility step's decision, and a permit from here would stand in for it.
            return Task.FromResult(GenericResult<StepOutcome>.Success(
                new StepOutcome.Contributed(new ContextContribution())));
        }

        AuthorizeSignInMethodLog.Refused(_logger, principal.Id, permission);
        return Task.FromResult(GenericResult<StepOutcome>.Success(
            new StepOutcome.Contributed(new ContextContribution
            {
                Decision = new Decision
                {
                    Permitted = false,
                    Reason = $"the principal does not hold '{permission}', which this sign-in method requires",
                },
            })));
    }
}
