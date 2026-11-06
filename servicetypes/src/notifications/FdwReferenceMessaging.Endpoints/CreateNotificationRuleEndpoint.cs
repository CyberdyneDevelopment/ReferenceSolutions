using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Notifications.Configuration;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Notifications.Endpoints;
using Fdw.Services.Settings;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace FdwReferenceNotifications.Endpoints;

/// <summary>
/// Endpoint to create a new notification rule configuration (POST /notifications/rules).
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateNotificationRuleEndpoint
    : CrudCreateEndpointBase<CreateNotificationRuleRequest, NotificationRuleSummaryDto>
{
    /// <summary>The domain these rules belong to, and the single implementation it dispatches to.</summary>
    private const string RuleDomain = "NotificationRule";

    /// <summary>The implementation name NotificationServiceTypes registers the rule provider under.</summary>
    private const string RuleImplementation = "NotificationRule";

    private readonly INotificationRuleConfigurationProvider _provider;
    private readonly Lazy<IEffectiveSettingsProvider> _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateNotificationRuleEndpoint"/> class.
    /// </summary>
    public CreateNotificationRuleEndpoint(
        ILogger<CreateNotificationRuleEndpoint> logger, INotificationRuleConfigurationProvider provider, Lazy<IEffectiveSettingsProvider> settings)
        : base(logger)
    {
        _provider = provider;
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc />
    // Why: route matches the existing ListNotificationRules route shape (GET /notifications/rules).
    protected override string ResourceName => "notifications/rules";

    /// <inheritdoc />
    protected override string GetResourceName(CreateNotificationRuleRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExists(
        CreateNotificationRuleRequest request,
        CancellationToken ct)
    {
        // Why Find and not Get(name): Get(name) fails when the row is absent, and absence is the
        // answer this asks for. Find reports an empty set instead, which is what "does not exist yet"
        // looks like.
        var existingResult = await _provider
            .Find<NotificationRuleImplementationConfiguration>(
                rule => string.Equals(rule.Name, request.Name, StringComparison.Ordinal), ct)
            .ConfigureAwait(false);
        return GenericResult<bool>.Success(existingResult.IsSuccess && existingResult.Value is { Count: > 0 });
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<NotificationRuleSummaryDto>> Create(
        CreateNotificationRuleRequest request,
        CancellationToken ct)
    {
        // What the request leaves out takes the caller's tenant's setting (FDW-3580): "make it configurable ... in configuration and not hardcoded".
        var tenantId = new ClaimsPrincipalAuthenticationContext(User).ActiveTenantId;
        var enabled = InstanceDefault.Resolve(
            request.IsEnabled, _settings, DefaultValueSettings.DefaultNotificationRuleIsEnabled, tenantId, "notification rule", request.Name, "IsEnabled", Logger);
        if (enabled.IsFailure)
        {
            return enabled.ToNewResult<NotificationRuleSummaryDto>();
        }

        var conditionOperator = InstanceDefault.ResolveText(
            request.ConditionOperator, _settings, DefaultValueSettings.DefaultNotificationRuleConditionOperator, tenantId, "notification rule", request.Name,
            "ConditionOperator", Logger);
        if (conditionOperator.IsFailure || conditionOperator.Value is not { } combine)
        {
            return GenericResult<NotificationRuleSummaryDto>.Failure(conditionOperator.NoValueReason());
        }

        // Why: Do not set Id/CreatedAt — Save assigns identity for new records (Id == default signals INSERT).
        var config = new NotificationRuleImplementationConfiguration
        {
            Name = request.Name,
            Description = request.Description,
            IsEnabled = enabled.Value,
            ConditionOperator = combine,
            ScheduleId = request.ScheduleId,
            PipelineId = request.PipelineId,
            WorkflowId = request.WorkflowId,
            NotificationServiceType = request.NotificationServiceType,
            NotificationServiceName = request.NotificationServiceName,
            Template = request.Template,
            Severity = request.Severity!,
            CooldownMinutes = request.CooldownMinutes
        };

        // The four arguments are the domain row's lookup-or-create key, not a restatement of the
        // configuration: name finds the existing domain row, domain and implementation populate a new
        // one when there is none, and that row's Id is stamped back onto config.
        var saveResult = await _provider
            .Save(config, RuleDomain, RuleImplementation, request.Name, ct)
            .ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<NotificationRuleSummaryDto>();
        }

        // Save writes the domain row's Id onto config rather than handing back a second copy, so the
        // record just written is the one to report.
        return GenericResult<NotificationRuleSummaryDto>.Success(new NotificationRuleSummaryDto
        {
            Id = config.Id,
            Name = request.Name,
            Description = config.Description,
            IsEnabled = config.IsEnabled,
            NotificationServiceType = config.NotificationServiceType,
            NotificationServiceName = config.NotificationServiceName,
            Severity = config.Severity
        });
    }

    /// <inheritdoc />
    protected override Task SendCreatedResponse(NotificationRuleSummaryDto detail, CancellationToken ct)
        => Send.ResponseAsync(detail, 201, ct);

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Notifications");
    }
}
