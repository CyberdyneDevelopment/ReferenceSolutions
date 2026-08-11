using Fdw.Services.Users;
using Fdw.Services.Users.Endpoints;
using Microsoft.Extensions.Logging;

namespace ReferenceUsers.Endpoints;

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
        ILoggerFactory loggerFactory)
        : base(preferenceProvider, loggerFactory)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Users");
    }
}
