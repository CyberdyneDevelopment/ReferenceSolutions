using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Messaging;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging;
using Reference.Api.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// Creates a new access request.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateAccessRequestEndpoint : CreateAccessRequestEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateAccessRequestEndpoint"/> class.
    /// </summary>
    public CreateAccessRequestEndpoint(IAccessRequestService accessRequestService, ILoggerFactory loggerFactory)
        : base(accessRequestService, loggerFactory)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Messages");
    }

    /// <inheritdoc/>
    protected override Task OnAccessRequestCreated(AccessRequestPayload dto, CancellationToken ct)
    {
        MessagingLog.AccessRequestCreated(EndpointLogger, dto.Id.ToString());
        return Send.CreatedAtAsync<ListAccessRequestsEndpoint>(null, dto, cancellation: ct);
    }
}
