using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;
using ReferenceAuthentication.OpenIddict;
using Fdw.Services.Authentication.OpenIddict;
using Fdw.Services.Authentication;
using Fdw.Services;
using Fdw;

namespace ReferenceAuthentication.OpenIddict.Logging;

/// <summary>MessageLogging for DataGateway-backed OpenIddict store operations. EventId range: 7276–7320.</summary>
[MessageLoggingTypeCode("OPENIDDICT")]
internal static partial class OpenIddictStoreLog
{
    // ── Scope store ─────────────────────────────────────────────────────────────────

    [MessageLogging(EventId = 11023, Level = LogLevel.Trace,
        Message = "OpenIddict scope CreateAsync starting for name '{name}'.")]
    internal static partial IGenericMessage ScopeCreateStarted(ILogger logger, string name);

    [MessageLogging(EventId = 11024, Level = LogLevel.Information,
        Message = "OpenIddict scope created: id={id} name='{name}'.")]
    internal static partial IGenericMessage ScopeCreated(ILogger logger, Guid id, string name);

    [MessageLogging(EventId = 71002, Level = LogLevel.Error,
        Message = "OpenIddict scope CreateAsync failed for name '{name}'.")]
    internal static partial IGenericMessage ScopeCreateFailed(ILogger logger, Exception exception, string name);

    [MessageLogging(EventId = 11025, Level = LogLevel.Trace,
        Message = "OpenIddict scope UpdateAsync starting for id={id}.")]
    internal static partial IGenericMessage ScopeUpdateStarted(ILogger logger, Guid id);

    [MessageLogging(EventId = 11026, Level = LogLevel.Information,
        Message = "OpenIddict scope updated: id={id}.")]
    internal static partial IGenericMessage ScopeUpdated(ILogger logger, Guid id);

    [MessageLogging(EventId = 71003, Level = LogLevel.Error,
        Message = "OpenIddict scope UpdateAsync failed for id={id}.")]
    internal static partial IGenericMessage ScopeUpdateFailed(ILogger logger, Exception exception, Guid id);

    [MessageLogging(EventId = 11027, Level = LogLevel.Trace,
        Message = "OpenIddict scope FindByIdAsync starting for id={id}.")]
    internal static partial IGenericMessage ScopeFindByIdStarted(ILogger logger, Guid id);

    [MessageLogging(EventId = 11028, Level = LogLevel.Trace,
        Message = "OpenIddict scope FindByNameAsync starting for name='{name}'.")]
    internal static partial IGenericMessage ScopeFindByNameStarted(ILogger logger, string name);

    [MessageLogging(EventId = 71004, Level = LogLevel.Error,
        Message = "OpenIddict scope query failed for id={id}.")]
    internal static partial IGenericMessage ScopeQueryFailed(ILogger logger, Exception exception, Guid id);

    [MessageLogging(EventId = 71005, Level = LogLevel.Error,
        Message = "OpenIddict scope query failed for name='{name}'.")]
    internal static partial IGenericMessage ScopeQueryByNameFailed(ILogger logger, Exception exception, string name);

    [MessageLogging(EventId = 11029, Level = LogLevel.Trace,
        Message = "OpenIddict scope GetResourcesAsync starting for id={id}.")]
    internal static partial IGenericMessage ScopeGetResourcesStarted(ILogger logger, Guid id);

    [MessageLogging(EventId = 11030, Level = LogLevel.Information,
        Message = "OpenIddict scope GetResourcesAsync returned {count} resource(s) for id={id}.")]
    internal static partial IGenericMessage ScopeGetResourcesComplete(ILogger logger, int count, Guid id);

    [MessageLogging(EventId = 71006, Level = LogLevel.Error,
        Message = "OpenIddict scope GetResourcesAsync failed for id={id}.")]
    internal static partial IGenericMessage ScopeGetResourcesFailed(ILogger logger, Exception exception, Guid id);

    [MessageLogging(EventId = 11031, Level = LogLevel.Trace,
        Message = "OpenIddict scope SetResourcesAsync starting for id={id} with {count} resource(s).")]
    internal static partial IGenericMessage ScopeSetResourcesStarted(ILogger logger, Guid id, int count);

    [MessageLogging(EventId = 11032, Level = LogLevel.Information,
        Message = "OpenIddict scope SetResourcesAsync complete: superseded old set, inserted {count} new resource(s) for id={id}.")]
    internal static partial IGenericMessage ScopeSetResourcesComplete(ILogger logger, int count, Guid id);

    [MessageLogging(EventId = 71007, Level = LogLevel.Error,
        Message = "OpenIddict scope SetResourcesAsync failed for id={id}.")]
    internal static partial IGenericMessage ScopeSetResourcesFailed(ILogger logger, Exception exception, Guid id);

    [MessageLogging(EventId = 71008, Level = LogLevel.Error,
        Message = "OpenIddict scope DeleteAsync failed for id={id}.")]
    internal static partial IGenericMessage ScopeDeleteFailed(ILogger logger, Exception exception, Guid id);

    // ── Application store ───────────────────────────────────────────────────────────

    [MessageLogging(EventId = 11033, Level = LogLevel.Trace,
        Message = "OpenIddict application CreateAsync starting for clientId='{clientId}'.")]
    internal static partial IGenericMessage ApplicationCreateStarted(ILogger logger, string clientId);

    [MessageLogging(EventId = 11034, Level = LogLevel.Information,
        Message = "OpenIddict application created: id={id} clientId='{clientId}'.")]
    internal static partial IGenericMessage ApplicationCreated(ILogger logger, Guid id, string clientId);

    [MessageLogging(EventId = 71009, Level = LogLevel.Error,
        Message = "OpenIddict application CreateAsync failed for clientId='{clientId}'.")]
    internal static partial IGenericMessage ApplicationCreateFailed(ILogger logger, Exception exception, string clientId);

    [MessageLogging(EventId = 11035, Level = LogLevel.Trace,
        Message = "OpenIddict application UpdateAsync starting for id={id}.")]
    internal static partial IGenericMessage ApplicationUpdateStarted(ILogger logger, Guid id);

    [MessageLogging(EventId = 11036, Level = LogLevel.Information,
        Message = "OpenIddict application updated: id={id}.")]
    internal static partial IGenericMessage ApplicationUpdated(ILogger logger, Guid id);

    [MessageLogging(EventId = 71010, Level = LogLevel.Error,
        Message = "OpenIddict application UpdateAsync failed for id={id}.")]
    internal static partial IGenericMessage ApplicationUpdateFailed(ILogger logger, Exception exception, Guid id);

    [MessageLogging(EventId = 71011, Level = LogLevel.Error,
        Message = "OpenIddict application DeleteAsync failed for id={id}.")]
    internal static partial IGenericMessage ApplicationDeleteFailed(ILogger logger, Exception exception, Guid id);

    // ── Authorization store ──────────────────────────────────────────────────────────

    [MessageLogging(EventId = 11037, Level = LogLevel.Trace,
        Message = "OpenIddict authorization CreateAsync starting.")]
    internal static partial IGenericMessage AuthorizationCreateStarted(ILogger logger);

    [MessageLogging(EventId = 11038, Level = LogLevel.Information,
        Message = "OpenIddict authorization created: id={id}.")]
    internal static partial IGenericMessage AuthorizationCreated(ILogger logger, Guid id);

    [MessageLogging(EventId = 71012, Level = LogLevel.Error,
        Message = "OpenIddict authorization CreateAsync failed.")]
    internal static partial IGenericMessage AuthorizationCreateFailed(ILogger logger, Exception exception);

    [MessageLogging(EventId = 11039, Level = LogLevel.Trace,
        Message = "OpenIddict authorization UpdateAsync starting for id={id}.")]
    internal static partial IGenericMessage AuthorizationUpdateStarted(ILogger logger, Guid id);

    [MessageLogging(EventId = 11040, Level = LogLevel.Information,
        Message = "OpenIddict authorization updated: id={id}.")]
    internal static partial IGenericMessage AuthorizationUpdated(ILogger logger, Guid id);

    [MessageLogging(EventId = 71013, Level = LogLevel.Error,
        Message = "OpenIddict authorization UpdateAsync failed for id={id}.")]
    internal static partial IGenericMessage AuthorizationUpdateFailed(ILogger logger, Exception exception, Guid id);

    [MessageLogging(EventId = 71014, Level = LogLevel.Error,
        Message = "OpenIddict authorization DeleteAsync failed for id={id}.")]
    internal static partial IGenericMessage AuthorizationDeleteFailed(ILogger logger, Exception exception, Guid id);

    [MessageLogging(EventId = 11041, Level = LogLevel.Information,
        Message = "OpenIddict authorization PruneAsync removed {count} authorization(s) with threshold={threshold}.")]
    internal static partial IGenericMessage AuthorizationPruned(ILogger logger, long count, DateTimeOffset threshold);

    // ── Revoked access token store ──────────────────────────────────────────────────

    [MessageLogging(EventId = 11042, Level = LogLevel.Trace,
        Message = "Revoking access token jti={jti}.")]
    internal static partial IGenericMessage RevokedTokenInsertStarted(ILogger logger, Guid jti);

    [MessageLogging(EventId = 11043, Level = LogLevel.Information,
        Message = "Access token jti={jti} revoked.")]
    internal static partial IGenericMessage RevokedTokenInserted(ILogger logger, Guid jti);

    [MessageLogging(EventId = 71015, Level = LogLevel.Error,
        Message = "Failed to revoke access token jti={jti}.")]
    internal static partial IGenericMessage RevokedTokenInsertFailed(ILogger logger, Exception exception, Guid jti);

    [MessageLogging(EventId = 11044, Level = LogLevel.Trace,
        Message = "Checking revocation status for access token jti={jti}.")]
    internal static partial IGenericMessage RevokedTokenCheckStarted(ILogger logger, Guid jti);

    [MessageLogging(EventId = 71016, Level = LogLevel.Error,
        Message = "Failed to check revocation status for access token jti={jti}.")]
    internal static partial IGenericMessage RevokedTokenCheckFailed(ILogger logger, Exception exception, Guid jti);

    // ── Shared / base ────────────────────────────────────────────────────────────────

    [MessageLogging(EventId = 71017, Level = LogLevel.Error,
        Message = "OpenIddict store DataGateway query returned a non-success result: {message}.")]
    internal static partial IGenericMessage DataGatewayQueryFailed(ILogger logger, string message);

    [MessageLogging(EventId = 71018, Level = LogLevel.Error,
        Message = "OpenIddict store DataGateway insert returned a non-success result: {message}.")]
    internal static partial IGenericMessage DataGatewayInsertFailed(ILogger logger, string message);

    [MessageLogging(EventId = 71019, Level = LogLevel.Error,
        Message = "OpenIddict store DataGateway update returned a non-success result: {message}.")]
    internal static partial IGenericMessage DataGatewayUpdateFailed(ILogger logger, string message);

    [MessageLogging(EventId = 71020, Level = LogLevel.Error,
        Message = "OpenIddict store DataGateway supersede (soft-delete current set) returned a non-success result: {message}.")]
    internal static partial IGenericMessage DataGatewaySupersedeFailed(ILogger logger, string message);

    [MessageLogging(EventId = 71021, Level = LogLevel.Error,
        Message = "OpenIddict store DataGateway delete returned a non-success result: {message}.")]
    internal static partial IGenericMessage DataGatewayDeleteFailed(ILogger logger, string message);
}
