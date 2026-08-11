using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace ReferenceMessages.Endpoints;

/// <summary>
/// Denies an access request.
/// </summary>
[ExcludeFromCodeCoverage]
public class DenyAccessRequestEndpoint : DenyAccessRequestEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DenyAccessRequestEndpoint"/> class.
    /// </summary>
    public DenyAccessRequestEndpoint(IAccessRequestService accessRequestService, ILoggerFactory loggerFactory)
        : base(accessRequestService, loggerFactory)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Messages");
    }
}
