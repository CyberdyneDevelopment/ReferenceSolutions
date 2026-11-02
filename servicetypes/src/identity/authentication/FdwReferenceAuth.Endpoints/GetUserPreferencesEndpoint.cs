using Fdw.Services.Users;
using Fdw.Services.Users.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint to get the current user's preferences.
/// </summary>
public class GetUserPreferencesEndpoint : GetUserPreferencesEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetUserPreferencesEndpoint"/> class.
    /// </summary>
    public GetUserPreferencesEndpoint(
        UserPreferenceConfigurationProvider preferenceProvider,
        ILogger<GetUserPreferencesEndpoint> logger)
        : base(preferenceProvider, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Users");
    }
}
