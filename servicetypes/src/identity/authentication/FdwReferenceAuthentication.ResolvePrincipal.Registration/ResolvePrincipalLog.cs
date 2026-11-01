using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuthentication.ResolvePrincipal.Registration;

/// <summary>
/// MessageLogging for binding a proved subject to a local principal, provisioning one
/// just-in-time when a deployment has configured that for the subject's issuer.
/// </summary>
/// <remarks>
/// EventId range: 91257-91261, 91310-91313. A subject identifier is never logged: it identifies a person at an
/// external authority, and an issuer plus a local principal id correlate as well without doing so.
/// The step-ran-before-Initialize failure is not here -- every step reports that through
/// <c>AuthenticationStepTypeBase.NotInitialized</c> with the same code, so a step in another
/// package never needs its own copy.
/// </remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class ResolvePrincipalLog
{
    /// <summary>An authenticated subject has no binding to any local principal.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The authority that asserted them.</param>
    [MessageLogging(EventId = 91257, Level = LogLevel.Warning,
        Message = "A subject asserted by '{issuer}' is bound to no local principal")]
    internal static partial IGenericMessage NoBinding(ILogger logger, string issuer);

    /// <summary>
    /// No tenant is marked global, so the platform's provisioning binding — the one the global tenant
    /// owns — cannot be found.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The authority whose subject was being provisioned.</param>
    [MessageLogging(EventId = 91258, Level = LogLevel.Error,
        Message = "Cannot provision a subject asserted by '{issuer}': no tenant is marked global, so there is no platform binding to consult")]
    internal static partial IGenericMessage GlobalTenantMissing(ILogger logger, string issuer);

    /// <summary>A subject was resolved to a local principal.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The authority that asserted them.</param>
    /// <param name="principalId">The local principal.</param>
    [MessageLogging(EventId = 91259, Level = LogLevel.Trace,
        Message = "Subject asserted by '{issuer}' resolved to principal {principalId}")]
    internal static partial IGenericMessage PrincipalResolved(
        ILogger logger, string issuer, Guid principalId);

    /// <summary>A subject with no binding was provisioned a new local principal.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The authority that asserted them.</param>
    /// <param name="provisionerName">The configured provisioner that created the principal.</param>
    /// <param name="principalId">The newly created local principal.</param>
    [MessageLogging(EventId = 91260, Level = LogLevel.Information,
        Message = "Subject asserted by '{issuer}' had no binding; provisioner '{provisionerName}' created principal {principalId}")]
    internal static partial IGenericMessage PrincipalProvisioned(
        ILogger logger, string issuer, string provisionerName, Guid principalId);

    /// <summary>A provisioner name resolved from a binding row does not resolve to a live provisioner.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The authority that asserted the subject.</param>
    /// <param name="provisionerName">The provisioner name the binding named.</param>
    [MessageLogging(EventId = 91261, Level = LogLevel.Error,
        Message = "Subject asserted by '{issuer}' is bound to provisioner '{provisionerName}', which did not resolve to a registered provisioner")]
    internal static partial IGenericMessage ProvisionerNotResolved(
        ILogger logger, string issuer, string provisionerName);

    /// <summary>The step's settings are not JSON.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="reason">The parser's complaint.</param>
    [MessageLogging(EventId = 91310, Level = LogLevel.Error,
        Message = "ResolvePrincipal settings are not JSON: {reason}")]
    internal static partial IGenericMessage ConfigurationNotJson(ILogger logger, string reason);

    /// <summary>The step's settings do not describe invitations completely.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91311, Level = LogLevel.Error,
        Message = "ResolvePrincipal settings must name Invitations.EmailClaim and exactly one of Invitations.EmailVerifiedClaim or Invitations.TrustIssuerEmail = true. A flow that does not redeem invitations carries no settings")]
    internal static partial IGenericMessage InvitationsIncomplete(ILogger logger);

    /// <summary>The issuer asserted no single address, so no invitation can be redeemed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The issuer.</param>
    /// <param name="count">How many distinct addresses it asserted.</param>
    [MessageLogging(EventId = 91312, Level = LogLevel.Debug,
        Message = "Issuer '{issuer}' asserted {count} distinct address(es); an invitation needs exactly one")]
    internal static partial IGenericMessage InvitationAddressUnusable(ILogger logger, string issuer, int count);

    /// <summary>The issuer asserted an address it did not verify.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The issuer.</param>
    // Information: an operator whose invited person cannot get in will look for exactly this.
    [MessageLogging(EventId = 91313, Level = LogLevel.Information,
        Message = "Issuer '{issuer}' asserted an address it did not mark verified, so no invitation was redeemed")]
    internal static partial IGenericMessage InvitationAddressUnverified(ILogger logger, string issuer);
}
