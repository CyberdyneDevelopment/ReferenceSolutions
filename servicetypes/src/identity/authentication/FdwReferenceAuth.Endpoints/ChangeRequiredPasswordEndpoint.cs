using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Users;
using Fdw.Services.Users.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FdwReferenceAuth.Endpoints.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Lets a user whose password must be changed — marked by their home tenant's admin, reset by the
/// platform administrator, or expired — set a new one, proving the current one (POST /auth/password).
/// </summary>
/// <remarks>
/// <para>
/// Anonymous, because the user cannot sign in: a login for such an account answers
/// <c>PasswordChangeRequired</c> and issues no token. Before FDW-825 there was no way out of that
/// state at all — the authenticated change-password endpoint needs a token, and the sign-in that
/// would produce one refuses — so marking an account locked it.
/// </para>
/// <para>
/// Runs as system: an anonymous caller can neither read nor write an account row, and what
/// authorizes the write is the proven password, not a session. Every refusal — unknown user,
/// wrong password, locked out — is the same answer, so this cannot be used to find usernames.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class ChangeRequiredPasswordEndpoint : Endpoint<ChangeRequiredPasswordRequest>
{
    private readonly UserConfigurationProvider _userProvider;
    private readonly IUserCredentialService _credentialService;
    private readonly IAuthenticationContextAccessor _authContextAccessor;
    private readonly ILogger<ChangeRequiredPasswordEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="ChangeRequiredPasswordEndpoint"/> class.</summary>
    public ChangeRequiredPasswordEndpoint(
        UserConfigurationProvider userProvider,
        IUserCredentialService credentialService,
        IAuthenticationContextAccessor authContextAccessor,
        ILogger<ChangeRequiredPasswordEndpoint> logger)
    {
        _userProvider = userProvider ?? throw new ArgumentNullException(nameof(userProvider));
        _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));
        _authContextAccessor = authContextAccessor ?? throw new ArgumentNullException(nameof(authContextAccessor));
        _logger = logger;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/auth/password");
        AllowAnonymous();
        Tags("Authentication");
        Summary(s =>
        {
            s.Summary = "Change a password that must be changed before signing in";
            s.Description = "For an account whose sign-in answered PasswordChangeRequired. Proves the current password and sets a new one; the next sign-in succeeds.";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(ChangeRequiredPasswordRequest req, CancellationToken ct)
    {
        AuthenticationLog.PasswordChangeRequested(_logger, req.Username);

        using var systemScope = new SystemAuthenticationContextScope(_authContextAccessor);

        var user = await _userProvider.Get(req.Username, ct).ConfigureAwait(false);
        if (!user.IsSuccess || user.Value is null)
        {
            await Refuse(req.Username, ct).ConfigureAwait(false);
            return;
        }

        // Credentials key on the user's durable id -- the Users domain row's.
        var userId = user.Value.Id;

        var verified = await _credentialService.Verify(userId, "Password", req.CurrentPassword, ct).ConfigureAwait(false);
        if (!verified.IsSuccess || verified.Value is not { ProvesSecret: true })
        {
            await Refuse(req.Username, ct).ConfigureAwait(false);
            return;
        }

        // Store also clears the must-change mark.
        var stored = await _credentialService.Store(userId, "Password", req.NewPassword, ct).ConfigureAwait(false);
        if (!stored.IsSuccess)
        {
            await Refuse(req.Username, ct).ConfigureAwait(false);
            return;
        }

        AuthenticationLog.PasswordChanged(_logger, req.Username);
        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }

    private async Task Refuse(string username, CancellationToken ct)
    {
        AuthenticationLog.PasswordChangeFailed(_logger, username);
        AddError("Password change failed. Verify the username and current password.");
        await Send.ErrorsAsync(StatusCodes.Status400BadRequest, ct).ConfigureAwait(false);
    }
}
