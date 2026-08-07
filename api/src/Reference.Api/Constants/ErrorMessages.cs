using System.Diagnostics.CodeAnalysis;

namespace Reference.Api.Constants;

[ExcludeFromCodeCoverage]
public static class ErrorMessages
{
    // Schema
    public const string UnsupportedConnectionType = "Unsupported connection type for schema discovery";
    public const string SchemaDiscoveryFailed = "Schema discovery failed";
    public const string UnsupportedConnectionTypeForSync = "Unsupported connection type for schema sync";
    public const string SchemaSyncFailed = "Schema sync failed";

    // Data preview
    public const string DataSetPreviewFailed = "Failed to preview data from DataSet";
    public const string TablePreviewFailed = "Failed to preview table data";

    // Promotion
    public const string FailedToListEnvironments = "Failed to list environments";
    public const string FailedToListPromotions = "Failed to list promotions";
    public const string FailedToGetPromotion = "Failed to get promotion";
    public const string FailedToCreatePromotion = "Failed to create promotion";
    public const string FailedToApprovePromotion = "Failed to approve promotion";
    public const string FailedToRejectPromotion = "Failed to reject promotion";
    public const string FailedToExecutePromotion = "Failed to execute promotion";
    public const string FailedToCompareEnvironments = "Failed to compare environments";

    // Calculations
    public const string NoValuesProvided = "No values provided for calculation";
    public const string CalculationInternalError = "An internal error occurred during calculation execution.";

    // Configuration instances
    public const string FailedToCreateConfigurationInstance = "Failed to create configuration instance";
    public const string FailedToUpdateConfigurationInstance = "Failed to update configuration instance";

    // Proxy
    public const string FailedToProxyScheduler = "Failed to proxy request to SchedulerServer";
    public const string FailedToProxyEtl = "Failed to proxy request to EtlServer";

    // Escalation
    public const string FailedToListEscalationPolicies = "Failed to list escalation policies";
    public const string FailedToGetEscalationPolicy = "Failed to get escalation policy";
    public const string FailedToCreateEscalationPolicy = "Failed to create escalation policy";
    public const string FailedToUpdateEscalationPolicy = "Failed to update escalation policy";
    public const string FailedToDeleteEscalationPolicy = "Failed to delete escalation policy";

    // Audit
    public const string FailedToListAuditRecords = "Failed to list audit records";
}
