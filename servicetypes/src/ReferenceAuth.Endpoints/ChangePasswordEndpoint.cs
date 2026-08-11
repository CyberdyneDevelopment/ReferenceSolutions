using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Endpoints;
using Fdw.Services.Users;
using Fdw.Services.Users.Abstractions;
using Microsoft.Extensions.Logging;
using ReferenceAuth.Endpoints.Logging;

namespace ReferenceAuth.Endpoints;

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
        ILoggerFactory loggerFactory)
        : base(loggerFactory)
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
        var userResult = await _userProvider.GetUser(username, ct).ConfigureAwait(false);
        if (!userResult.IsSuccess || userResult.Value is null)
            return userResult.Messages.Any()
                ? (IGenericResult)GenericResult.Failure(userResult.Messages.ToArray())
                : GenericResult.Failure(AuthenticationLog.AuthenticationFailed(EndpointLogger, username));
        var user = userResult.Value;

        var verifyResult = await _credentialService.Verify(user.Id, "Password", currentPassword, ct).ConfigureAwait(false);
        if (!verifyResult.IsSuccess)
            return verifyResult;
        var verifyValue = verifyResult.Value;
        if (verifyValue is null || !verifyValue.GrantsAccess)
            return GenericResult.Failure(AuthenticationLog.InvalidCredentials(EndpointLogger, username));

        return await _credentialService.Store(user.Id, "Password", newPassword, ct).ConfigureAwait(false);
    }
}
