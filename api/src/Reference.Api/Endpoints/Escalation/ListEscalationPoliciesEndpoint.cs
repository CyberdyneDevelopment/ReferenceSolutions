using Fdw.Operations.Abstractions.Escalation;
using Fdw.Operations.Endpoints.Escalation;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints.Escalation;

/// <summary>
/// Lists all escalation policies.
/// </summary>
public sealed class ListEscalationPoliciesEndpoint : ListEscalationPoliciesEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListEscalationPoliciesEndpoint"/> class.
    /// </summary>
    public ListEscalationPoliciesEndpoint(IEscalationService escalationService, ILogger<ListEscalationPoliciesEndpointBase>? logger)
        : base(escalationService, logger) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("Escalation");
}
