using Fdw.Operations.Abstractions.Escalation;
using Fdw.Operations.Endpoints.Escalation;
using Microsoft.Extensions.Logging;

namespace ReferenceEscalation.Endpoints;

/// <summary>
/// Gets an escalation policy by ID.
/// </summary>
public sealed class GetEscalationPolicyEndpoint : GetEscalationPolicyEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetEscalationPolicyEndpoint"/> class.
    /// </summary>
    public GetEscalationPolicyEndpoint(IEscalationService escalationService, ILogger<GetEscalationPolicyEndpointBase>? logger)
        : base(escalationService, logger) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("Escalation");
}
