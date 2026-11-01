using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMessages.Endpoints;

/// <summary>
/// Approves an access request.
/// </summary>
[ExcludeFromCodeCoverage]
public class ApproveAccessRequestEndpoint : ApproveAccessRequestEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApproveAccessRequestEndpoint"/> class.
    /// </summary>
    public ApproveAccessRequestEndpoint(IAccessRequestService accessRequestService, ILogger<ApproveAccessRequestEndpoint> logger)
        : base(accessRequestService, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Messages");
    }
}
