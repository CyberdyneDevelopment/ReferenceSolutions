using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authentication.Abstractions.SignIns;
using Fdw.Services.Users;
using Fdw.Services.Users.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// List an account's sign-ins.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListUserSignInsEndpoint : ListUserSignInsEndpointBase
{
    /// <inheritdoc />
    public ListUserSignInsEndpoint(
        IUserConfigurationProvider userProvider,
        ISignInLog signIns,
        ILogger<ListUserSignInsEndpoint> logger)
        : base(logger, userProvider, signIns)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Summary(s =>
        {
            s.Summary = "List an account's sign-ins";
            s.Description = "Newest first: when, the service and flow, the address, the browser and the operating system.";
        });
        Tags("Users");
    }
}
