using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Endpoints;
using Fdw.Services.Users;
using Fdw.Services.Users.Abstractions;
using Microsoft.Extensions.Logging;
using FdwReferenceAuth.Endpoints.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint to change the current user's password.
/// </summary>
public class ChangePasswordEndpoint : ChangePasswordEndpointBase
{
    private readonly UserConfigurationProvider _userProvider;
    private readonly IUserCredentialService _credentialService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangePasswordEndpoint"/> class.
    /// </summary>
    public ChangePasswordEndpoint(
        UserConfigurationProvider userProvider,
        IUserCredentialService credentialService,
        ILogger<ChangePasswordEndpoint> logger)
        : base(logger)
    {
        _userProvider = userProvider;
        _credentialService = credentialService;
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Authentication");
    }

    /// <inheritdoc/>
    protected override async Task<IGenericResult> PerformChangePassword(
        string username, string currentPassword, string newPassword, CancellationToken ct)
    {
        // The base hands over the caller's identity name, which for a token this platform issued is
        // the subject -- the user's id -- not their username (the handler's NameClaimType is sub).
        // Looked up by name, that id matched nobody and every password change failed.
        var userResult = Guid.TryParse(username, out var callerId)
            ? await _userProvider.Get(callerId, ct).ConfigureAwait(false)
            : await _userProvider.Get(username, ct).ConfigureAwait(false);
        if (!userResult.IsSuccess)
            return GenericResult.Failure(userResult.Messages.ToArray());
        if (userResult.Value is null)
            return GenericResult.Failure(AuthenticationLog.AuthenticationFailed(EndpointLogger, username));

        // Credentials key on the user's durable id -- the Users domain row's, which the provider
        // stamps onto the implementation configuration as its Id.
        var userId = userResult.Value.Id;

        var verifyResult = await _credentialService.Verify(userId, "Password", currentPassword, ct).ConfigureAwait(false);
        if (!verifyResult.IsSuccess)
            return verifyResult;
        var verifyValue = verifyResult.Value;
        // Proof of the current password, not permission to sign in with it: a password that is due
        // for change (expired, or the account marked to change it) must still be changeable.
        if (verifyValue is null || !verifyValue.ProvesSecret)
            return GenericResult.Failure(AuthenticationLog.InvalidCredentials(EndpointLogger, username));

        return await _credentialService.Store(userId, "Password", newPassword, ct).ConfigureAwait(false);
    }
}
