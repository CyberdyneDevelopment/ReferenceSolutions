using Fdw.Services.Users;
using Fdw.Services.Users.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

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
        ILogger<UpdateUserPreferencesEndpoint> logger)
        : base(preferenceProvider, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Users");
    }
}
