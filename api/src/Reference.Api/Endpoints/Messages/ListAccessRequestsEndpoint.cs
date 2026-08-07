using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// Lists access requests. Admins see all pending; users see their own.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListAccessRequestsEndpoint : ListAccessRequestsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListAccessRequestsEndpoint"/> class.
    /// </summary>
    public ListAccessRequestsEndpoint(IAccessRequestService accessRequestService, ILoggerFactory loggerFactory)
        : base(accessRequestService, loggerFactory)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Messages");
    }
}
