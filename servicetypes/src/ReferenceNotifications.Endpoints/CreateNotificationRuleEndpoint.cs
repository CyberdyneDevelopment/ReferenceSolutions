using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Notifications.Configuration;
using Fdw.Services.Notifications.Endpoints;
using Fdw.Web.RestEndpoints.Crud;

namespace ReferenceNotifications.Endpoints;

/// <summary>
/// Endpoint to create a new notification rule configuration (POST /notifications/rules).
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateNotificationRuleEndpoint
    : CrudCreateEndpoint<CreateNotificationRuleRequest, NotificationRuleSummaryDto>
{
    private readonly IServiceConfigurationProvider<NotificationRuleConfiguration> _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateNotificationRuleEndpoint"/> class.
    /// </summary>
    public CreateNotificationRuleEndpoint(IServiceConfigurationProvider<NotificationRuleConfiguration> provider)
    {
        _provider = provider;
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
        var existingResult = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        return GenericResult<bool>.Success(existingResult.IsSuccess && existingResult.Value != null);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<NotificationRuleSummaryDto>> Create(
        CreateNotificationRuleRequest request,
        CancellationToken ct)
    {
        // Why: Do not set Id/CreatedAt — Save assigns identity for new records (Id == default signals INSERT).
        var config = new NotificationRuleConfiguration
        {
            Name = request.Name,
            Description = request.Description,
            IsEnabled = request.IsEnabled,
            ScheduleId = request.ScheduleId,
            PipelineId = request.PipelineId,
            WorkflowId = request.WorkflowId,
            ConditionOperator = request.ConditionOperator,
            NotificationServiceType = request.NotificationServiceType,
            NotificationServiceName = request.NotificationServiceName,
            Template = request.Template,
            Severity = request.Severity,
            CooldownMinutes = request.CooldownMinutes
        };

        var saveResult = await _provider.Save(config, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<NotificationRuleSummaryDto>();
        }

        var saved = saveResult.Value!;
        return GenericResult<NotificationRuleSummaryDto>.Success(new NotificationRuleSummaryDto
        {
            Id = saved.Id,
            Name = saved.Name,
            Description = saved.Description,
            IsEnabled = saved.IsEnabled,
            NotificationServiceType = saved.NotificationServiceType,
            NotificationServiceName = saved.NotificationServiceName,
            Severity = saved.Severity
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
