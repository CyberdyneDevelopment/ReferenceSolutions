using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Denies an access request.
/// </summary>
[ExcludeFromCodeCoverage]
public class DenyAccessRequestEndpoint : DenyAccessRequestEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DenyAccessRequestEndpoint"/> class.
    /// </summary>
    public DenyAccessRequestEndpoint(IAccessRequestService accessRequestService, ILogger<DenyAccessRequestEndpoint> logger)
        : base(accessRequestService, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Messages");
    }
}
