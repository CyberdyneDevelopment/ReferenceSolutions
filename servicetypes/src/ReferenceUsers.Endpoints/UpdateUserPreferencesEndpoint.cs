using Fdw.Services.Users;
using Fdw.Services.Users.Endpoints;
using Microsoft.Extensions.Logging;

namespace ReferenceUsers.Endpoints;

/// <summary>
/// Endpoint to update the current user's preferences.
/// </summary>
public class UpdateUserPreferencesEndpoint : UpdateUserPreferencesEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateUserPreferencesEndpoint"/> class.
    /// </summary>
    public UpdateUserPreferencesEndpoint(
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
