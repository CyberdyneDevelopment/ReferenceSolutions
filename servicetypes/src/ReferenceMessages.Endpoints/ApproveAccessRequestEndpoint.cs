using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace ReferenceMessages.Endpoints;

/// <summary>
/// Approves an access request.
/// </summary>
[ExcludeFromCodeCoverage]
public class ApproveAccessRequestEndpoint : ApproveAccessRequestEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApproveAccessRequestEndpoint"/> class.
    /// </summary>
    public ApproveAccessRequestEndpoint(IAccessRequestService accessRequestService, ILoggerFactory loggerFactory)
        : base(accessRequestService, loggerFactory)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Messages");
    }
}
