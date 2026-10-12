using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Users.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FdwReferenceAuth.Endpoints.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Sets a user's password from a one-time set-password link (POST /auth/password-link, FDW-941).
/// </summary>
/// <remarks>
/// <para>
/// Anonymous, because the person opening the link may have no password at all, or have forgotten it:
/// the link is the authority, made by the platform administrator for exactly this user.
/// </para>
/// <para>
/// The link is used up before the password is stored, in one atomic step, so the same link can never set
/// a password twice. Every refusal -- unknown, used, revoked or expired -- is the same answer, so this
/// cannot be used to probe for links. A password that fails the request rules is refused before the link
/// is touched, so it still works for a second try.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class RedeemPasswordLinkEndpoint : Endpoint<RedeemPasswordLinkRequest>
{
    private readonly IPasswordLinkService _linkService;
    private readonly IUserCredentialService _credentialService;
    private readonly IAuthenticationContextAccessor _authContextAccessor;
    private readonly ILogger<RedeemPasswordLinkEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="RedeemPasswordLinkEndpoint"/> class.</summary>
    public RedeemPasswordLinkEndpoint(
        IPasswordLinkService linkService,
        IUserCredentialService credentialService,
        IAuthenticationContextAccessor authContextAccessor,
        ILogger<RedeemPasswordLinkEndpoint> logger)
    {
        _linkService = linkService ?? throw new ArgumentNullException(nameof(linkService));
        _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));
        _authContextAccessor = authContextAccessor ?? throw new ArgumentNullException(nameof(authContextAccessor));
        _logger = logger;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/auth/password-link");
        AllowAnonymous();
        Tags("Authentication");
        Summary(s =>
        {
            s.Summary = "Set a password from a one-time password link";
            s.Description = "Takes the token from a link an administrator created and the new password. The link works once, until it expires.";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(RedeemPasswordLinkRequest req, CancellationToken ct)
    {
        // Runs as system: an anonymous caller can neither read nor write an account row, and what
        // authorizes the write is the link, not a session -- as for ChangeRequiredPasswordEndpoint.
        using var systemScope = new SystemAuthenticationContextScope(_authContextAccessor);

        var redeemed = await _linkService.Redeem(req.Token, ct).ConfigureAwait(false);

        // A service failure is the server's fault, not the link's: a new link would not help.
        if (!redeemed.IsSuccess || redeemed.Value is null)
        {
            AuthenticationLog.PasswordLinkNotChecked(_logger);
            AddError("The link could not be checked right now. Try again later.");
            await Send.ErrorsAsync(StatusCodes.Status500InternalServerError, ct).ConfigureAwait(false);
            return;
        }

        if (redeemed.Value is not { IsValid: true } link)
        {
            AuthenticationLog.PasswordLinkRefused(_logger);
            AddError("This link does not work. It may have been used already or expired; ask your administrator for a new one.");
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, ct).ConfigureAwait(false);
            return;
        }

        // Store also clears any must-change mark: the password was chosen by its owner.
        var stored = await _credentialService.Store(link.UserId, "Password", req.NewPassword, ct).ConfigureAwait(false);
        if (!stored.IsSuccess)
        {
            AuthenticationLog.PasswordLinkPasswordNotStored(_logger, link.UserId);
            AddError("The password could not be set, and the link has been used. Ask your administrator for a new one.");
            await Send.ErrorsAsync(StatusCodes.Status500InternalServerError, ct).ConfigureAwait(false);
            return;
        }

        AuthenticationLog.PasswordLinkUsed(_logger, link.UserId);
        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
