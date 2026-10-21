using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Flow;
using Fdw.Services.Authentication.Sessions;
using FdwReferenceAuth.Endpoints.Logging;
using FdwReferenceAuthentication.TenantSwitch.Registration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Re-issues a caller's token by running the TenantSwitch authentication flow, for the switch of tenant and the switch of department
/// alike (FDW-1802): the same flow, so the same checks, and a caller signed in by a session stays one.
/// </summary>
internal sealed class SwitchFlowRunner
{
    private const string FlowName = "TenantSwitch";

    // A caller signed in by a server-side session stays one after switching (FDW-903): its switch runs
    // the session-delivered twin, so no bearer token is ever handed to the browser app.
    private const string SessionFlowName = "TenantSwitchSession";

    private readonly AuthenticationRunner _runner;
    private readonly IAuthenticationFlowProvider _flows;
    private readonly ISessionCookieDelivery _sessions;
    private readonly ILogger _logger;

    internal SwitchFlowRunner(AuthenticationRunner runner, IAuthenticationFlowProvider flows, ISessionCookieDelivery sessions, ILogger logger)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _flows = flows ?? throw new ArgumentNullException(nameof(flows));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // The token for the tenant, and the department when one is named; the department's absence is a switch of tenant alone.
    internal async Task<IGenericResult<(string AccessToken, DateTimeOffset ExpiresAt)>> Run(
        HttpContext httpContext, string userId, Guid tenantId, Guid? organizationId, CancellationToken cancellationToken)
    {
        // Handed to the step through the request, the same way the password step receives its
        // credential: a step's signature is fixed by the pipeline.
        httpContext.Items[HttpTenantSwitchRequestAccessor.RequestedTenantItemKey] = tenantId;
        if (organizationId is { } department)
        {
            httpContext.Items[HttpTenantSwitchRequestAccessor.RequestedOrganizationItemKey] = department;
        }

        var flowName = _sessions.IsSessionRequest(httpContext) ? SessionFlowName : FlowName;
        var flow = await _flows.Get(flowName, cancellationToken).ConfigureAwait(false);
        if (flow.IsFailure || flow.Value is null)
        {
            return GenericResult<(string, DateTimeOffset)>.Failure(
                TenantLog.TenantSwitchNotIssued(_logger, userId, tenantId, $"no flow named '{flowName}' is configured"));
        }

        var result = await _runner.Run(flow.Value, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return result.ToNewResult<(string, DateTimeOffset)>();
        }

        // TenantSwitch never suspends; reaching Suspended or Waiting means the flow was configured
        // with a step this endpoint cannot serve.
        if (result.Value is not FlowResult.Completed completed)
        {
            return GenericResult<(string, DateTimeOffset)>.Failure(
                TenantLog.TenantSwitchNotIssued(_logger, userId, tenantId, "the flow did not complete"));
        }

        if (flow.Value.SessionServiceName is null)
        {
            return GenericResult<(string, DateTimeOffset)>.Success((completed.Token.AccessToken, completed.Token.ExpiresAt));
        }

        var replaced = await _sessions.Replace(httpContext, flow.Value, completed.Token, cancellationToken).ConfigureAwait(false);
        if (replaced.IsFailure)
        {
            return replaced.ToNewResult<(string, DateTimeOffset)>();
        }

        // The new session id is in the cookie Replace set. The response body carries no credential for
        // a session caller, so the access token it reports is empty by design, not by omission.
        return GenericResult<(string, DateTimeOffset)>.Success((string.Empty, completed.Token.ExpiresAt));
    }
}
