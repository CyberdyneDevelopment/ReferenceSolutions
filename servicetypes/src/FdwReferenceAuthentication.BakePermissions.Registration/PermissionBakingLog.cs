using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuthentication.BakePermissions.Registration;

/// <summary>
/// MessageLogging for baking a principal's permissions into a token.
/// </summary>
/// <remarks>EventId range: 91244–91249, 91251.</remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class PermissionBakingLog
{
    /// <summary>Permissions were resolved and staged for the token.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="principalId">Whose they are.</param>
    /// <param name="count">How many.</param>
    // The count, not the permissions: the list is long enough to bury every other line at Trace,
    // and the count is what answers the question actually asked of this log - whether baking
    // happened at all. A token carrying zero permissions is the failure worth seeing.
    [MessageLogging(EventId = 91245, Level = LogLevel.Information,
        Message = "Baked {count} permission(s) for principal {principalId}")]
    internal static partial IGenericMessage Baked(
        ILogger logger, Guid principalId, int count);

    /// <summary>The actual permission and role names baked for the token, alongside <see cref="Baked"/>.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="principalId">Whose they are.</param>
    /// <param name="permissions">The resolved permission names.</param>
    /// <param name="roles">The resolved role names.</param>
    // Trace, not Information: this is the list Baked's own comment says would bury every other
    // line at a milestone level. It still needs to exist somewhere -- "why doesn't this token
    // carry permission X" has no answer without it -- so it lives one level down, alongside every
    // other per-item detail this framework logs at Trace.
    [MessageLogging(EventId = 91248, Level = LogLevel.Trace,
        Message = "Principal {principalId} baked permissions [{permissions}] and roles [{roles}]")]
    internal static partial IGenericMessage BakedDetail(
        ILogger logger, Guid principalId, string permissions, string roles);

    /// <summary>Logs a step asked to run before its Initialize captured what it needs.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="stepName">The step the flow named.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 91246, Level = LogLevel.Error,
        Message = "Step '{stepName}' ran before its dependencies were captured, so it cannot do its work. The option's Initialize phase did not run, which means the host was not fully initialized before a flow reached this step")]
    internal static partial IGenericMessage NotInitialized(ILogger logger, string stepName);
    /// <summary>Tenant lookup returned no tenant for permission resolution.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="tenantId">The tenant required by the principal.</param>
    /// <returns>The structured failure.</returns>
    [MessageLogging(EventId = 91247, Level = LogLevel.Error,
        Message = "Tenant {tenantId} was not returned for permission baking; token issuance cannot continue")]
    internal static partial IGenericMessage TenantMissing(ILogger logger, Guid tenantId);

    /// <summary>Permission resolution succeeded but returned no authorization to bake.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="principalId">Whose permissions were resolved.</param>
    /// <param name="tenantId">The tenant they were resolved for.</param>
    /// <returns>The structured failure.</returns>
    [MessageLogging(EventId = 91244, Level = LogLevel.Error,
        Message = "Permission resolution for principal {principalId} in tenant {tenantId} returned no result; a token cannot be issued without knowing what it carries")]
    internal static partial IGenericMessage ResolvedNothing(ILogger logger, Guid principalId, Guid tenantId);

    /// <summary>No tenant is marked global, so a global member's roles cannot be located.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The structured failure.</returns>
    [MessageLogging(EventId = 91249, Level = LogLevel.Error,
        Message = "No tenant is marked IsGlobal; whether the principal acts with global-tenant roles cannot be decided")]
    internal static partial IGenericMessage GlobalTenantMissing(ILogger logger);

    /// <summary>A global-tenant member acting as another tenant carries its global-tenant roles there.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="principalId">The global member.</param>
    /// <param name="tenantId">The tenant it is acting as.</param>
    /// <param name="roles">The global-tenant roles applied.</param>
    /// <returns>The structured message.</returns>
    // Information: which roles a token carries, and why, is the question every "why can this
    // caller do X in tenant Y" starts from; this is the one case where the answer is not "the roles
    // held in Y".
    [MessageLogging(EventId = 91251, Level = LogLevel.Information,
        Message = "Principal {principalId} is a member of the global tenant; acting as tenant {tenantId} it carries its global-tenant roles [{roles}]")]
    internal static partial IGenericMessage GlobalRolesApplied(ILogger logger, Guid principalId, Guid tenantId, string roles);
}