using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Authentication.Flow;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Users;
using Fdw.Services.Users.Results;
using Fdw.Services.Users.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceAuthentication.PasswordCredential.Registration;

/// <summary>
/// Proves a caller by the password they hold, against this platform's own credential store.
/// </summary>
/// <remarks>
/// <para>
/// The one step that both proves and identifies. Every federated step splits those in two — an
/// authority proves a subject, and the ResolvePrincipal step binds that subject to a principal —
/// because the authority does not know our principals. Here the credential is checked against a row
/// that already IS the principal, so a binding step would have nothing to look up.
/// </para>
/// <para>
/// The password reaches this step and stops here. It is read from the accessor at the moment of
/// verification and never contributed: a credential on the context is a credential every later step
/// can read and anything logging the context can print.
/// </para>
/// <para>
/// The option IS the step. A flow names it, the collection answers by that name, and what answers
/// is the thing that runs — so there is no second mapping from a name to a type to keep in step
/// with this one.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[Implementation(typeof(AuthenticationStepTypes), "PasswordCredential")]
public sealed class PasswordCredentialStepType
    : AuthenticationStepTypeBase<IGenericService, IServiceFactory<IGenericService, IServiceConfiguration>>,
      IAuthenticationStep
{
    // Captured when the host is built rather than taken through the constructor: an option is
    // created by its module initializer, which needs a parameterless constructor, so what it needs
    // arrives in Initialize where a live container exists.
    private IUserConfigurationProvider? _users;
    private IUserCredentialService? _credentials;
    private IPasswordCredentialAccessor? _presented;
    private ITenantResolver? _tenants;
    private IAuthenticationContextAccessor? _authContextAccessor;
    private ILogger _logger = NullLogger<PasswordCredentialStepType>.Instance;

    /// <summary>Initializes a new instance of the <see cref="PasswordCredentialStepType"/> class.</summary>
    public PasswordCredentialStepType()
        : base("PasswordCredential",
               "AuthenticationSteps",
               "Password Credential",
               "Verifies a username and password against this host's own credential store")
    {
        Registration((builder, loggerFactory) =>
        {
            // What this step reads the presented credential through. Registered here, not by the
            // shared collection, so a host that never references this package never pulls it in.
            // One ambient accessor for every transport: an HTTP login and a SQL Server login each
            // present what they received through a PresentedPasswordCredentialScope around the flow.
            builder.Services.TryAddSingleton<PresentedPasswordCredentialAccessor>();
            builder.Services.TryAddSingleton<IPasswordCredentialAccessor>(sp => sp.GetRequiredService<PresentedPasswordCredentialAccessor>());

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Initialization((host, loggerFactory) =>
        {
            _authContextAccessor = host.Services.GetRequiredService<IAuthenticationContextAccessor>();
            var services = host.Services;

            _users = services.GetRequiredService<IUserConfigurationProvider>();
            _credentials = services.GetRequiredService<IUserCredentialService>();
            _presented = services.GetRequiredService<IPasswordCredentialAccessor>();
            _tenants = services.GetRequiredService<ITenantResolver>();
            _logger = loggerFactory?.CreateLogger<PasswordCredentialStepType>()
                ?? NullLogger<PasswordCredentialStepType>.Instance;

            return GenericResult<IHost>.Success(host);
        });
    }

    /// <inheritdoc />
    public IReadOnlyList<IContextElement> Requires => [];

    /// <inheritdoc />
    public IReadOnlyList<IContextElement> Contributes => [ContextElements.Subject, ContextElements.Principal];

    /// <inheritdoc />
    /// <remarks>RFC 8176: a password is a knowledge factor.</remarks>
    public IReadOnlyList<string> AuthenticationMethods => ["pwd"];

    /// <inheritdoc />
    public async Task<IGenericResult<StepOutcome>> Execute(
        AuthenticationContext context, ITenantProvider tenants, CancellationToken cancellationToken = default)
    {
        // An option whose Initialize never ran has nothing to verify against, and admitting a caller
        // on that basis would be the worst possible reading of a missing dependency.
        if (_users is null || _credentials is null || _presented is null || _tenants is null || _authContextAccessor is null)
            return GenericResult<StepOutcome>.Failure(NotInitialized(_logger));

        var username = _presented.Username;
        var password = _presented.Password;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            return GenericResult<StepOutcome>.Failure(PasswordCredentialLog.NothingPresented(_logger));

        // Only this trusted built-in authentication operation runs under system context.
        using var systemScope = new SystemAuthenticationContextScope(_authContextAccessor);

        var found = await _users.Get(username, cancellationToken).ConfigureAwait(false);
        if (found.IsFailure)
            return found.ToNewResult<StepOutcome>();

        // Why the same refusal for an unknown user and a wrong password: the two are
        // indistinguishable to the caller by design. A distinct message here is an account
        // enumeration oracle - it tells an attacker which usernames are worth attacking.
        if (found.Value is not { IsActive: true })
            return GenericResult<StepOutcome>.Failure(PasswordCredentialLog.Refused(_logger, username));

        var user = found.Value;

        var verified = await _credentials
            .Verify(user.Id, "Password", password, cancellationToken)
            .ConfigureAwait(false);

        // Verify composes an outcome rather than returning a bool: NoMatch, Expired, MustChange and
        // TooManyAttempts are all distinct states that deny access. Only GrantsAccess admits, so
        // asking that one question keeps every future denial state denying without this step
        // learning about it.
        if (!verified.IsSuccess)
            return GenericResult<StepOutcome>.Failure(PasswordCredentialLog.Refused(_logger, username));

        // The password was right but must be changed first (marked at next login, or expired). Said
        // so, rather than refused like a wrong password: it is only reachable after the password was
        // proved, so it tells an attacker nothing, and a caller that cannot tell the two apart has no
        // way to send the user to change it -- the account would simply stop working.
        if (verified.Value is { GrantsAccess: false, ProvesSecret: true })
            return GenericResult<StepOutcome>.Failure(UserResultCodes.ByName("PasswordChangeRequired"), _logger);

        if (verified.Value is not { GrantsAccess: true })
            return GenericResult<StepOutcome>.Failure(PasswordCredentialLog.Refused(_logger, username));

        var tenant = await _tenants.TenantFor(user.Id, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
            return tenant.ToNewResult<StepOutcome>();

        PasswordCredentialLog.Proved(_logger, user.Id);

        return GenericResult<StepOutcome>.Success(new StepOutcome.Contributed(new ContextContribution
        {
            Subject = new Subject
            {
                Issuer = AuthConstants.LocalIssuer,
                SubjectId = user.Id.ToString(),
                AuthenticatedAt = DateTimeOffset.UtcNow,
            },

            Principal = new Principal { Id = user.Id, TenantId = tenant.Value },
        }));
    }
}
