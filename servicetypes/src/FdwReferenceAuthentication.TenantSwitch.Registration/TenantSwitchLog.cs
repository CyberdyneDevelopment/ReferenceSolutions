using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuthentication.TenantSwitch.Registration;

/// <summary>
/// MessageLogging for re-issuing a caller's token for another of their tenants.
/// </summary>
/// <remarks>
/// EventId range: 91253-91255. The step-ran-before-Initialize failure is not here -- every step
/// reports that through <c>AuthenticationStepTypeBase.NotInitialized</c> with the same code, so a
/// step in another package never needs its own copy.
/// </remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class TenantSwitchLog
{
    /// <summary>The request carried no authenticated caller.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91253, Level = LogLevel.Warning,
        Message = "A tenant switch was asked for by a request with no authenticated caller")]
    internal static partial IGenericMessage NoCaller(ILogger logger);

    /// <summary>The request named no tenant to switch to.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="callerId">Who asked.</param>
    [MessageLogging(EventId = 91254, Level = LogLevel.Warning,
        Message = "Principal {callerId} asked to switch tenant without naming one")]
    internal static partial IGenericMessage NoTenant(ILogger logger, Guid callerId);

    /// <summary>A caller is being re-issued for a tenant.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="callerId">Who asked.</param>
    /// <param name="tenantId">The tenant they asked to act in.</param>
    [MessageLogging(EventId = 91255, Level = LogLevel.Information,
        Message = "Principal {callerId} is switching to tenant {tenantId}")]
    internal static partial IGenericMessage Switching(ILogger logger, Guid callerId, Guid tenantId);
}
