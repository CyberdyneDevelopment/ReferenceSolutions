using System.Diagnostics.CodeAnalysis;
using FastEndpoints;
using FluentValidation;
using Fdw.Services.Pipelines.Endpoints;

namespace FdwReferencePipelines.Endpoints.Validators;

/// <summary>
/// Validator for <see cref="ExecutePipelineRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ExecutePipelineRequestValidator : Validator<ExecutePipelineRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutePipelineRequestValidator"/> class.
    /// </summary>
    public ExecutePipelineRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Pipeline name is required");
    }
}

/// <summary>
/// Validator for <see cref="GetPipelineStatusRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetPipelineStatusRequestValidator : Validator<GetPipelineStatusRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetPipelineStatusRequestValidator"/> class.
    /// </summary>
    public GetPipelineStatusRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Pipeline name is required");
    }
}

/// <summary>
/// Validator for <see cref="PipelineNameRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class PipelineNameRequestValidator : Validator<PipelineNameRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineNameRequestValidator"/> class.
    /// </summary>
    public PipelineNameRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Pipeline name is required");
    }
}

/// <summary>
/// Validator for <see cref="CreatePipelineRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreatePipelineRequestValidator : Validator<CreatePipelineRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreatePipelineRequestValidator"/> class.
    /// </summary>
    public CreatePipelineRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Pipeline name is required")
            .MaximumLength(100)
            .WithMessage("Pipeline name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z][a-zA-Z0-9_-]*$")
            .WithMessage("Pipeline name must start with a letter and contain only letters, numbers, underscores, or hyphens");

        RuleFor(x => x.PipelineType)
            .NotEmpty()
            .WithMessage("Pipeline type is required (e.g., BatchCopy, Streaming)");

        // Why: the BatchCopy factory requires EXACTLY ONE of {DataSet, ConnectionName} per source and
        // per sink — a DataSet carries its own connection binding, a raw connection is the alternative.
        // Forcing a connection name produced configs with BOTH set, which the factory rejects
        // ("exactly one is required") so the pipeline could never execute. Mirror the factory contract here.
        // A data store container is the third source form (FDW-1103): store, path and container, all three.
        RuleFor(x => x)
            .Must(r => PipelineSourceRules.SourceForms(r.SourceDataSet, r.SourceConnectionName, r.SourceDataStoreName) == 1)
            .WithMessage("Exactly one of SourceDataSet, SourceConnectionName or SourceDataStoreName (with SourceDataStorePath and SourceContainerName) must be supplied");

        RuleFor(x => x)
            .Must(r => PipelineSourceRules.DataStoreSourceIsWhole(r.SourceDataStoreName, r.SourceDataStorePath, r.SourceContainerName))
            .WithMessage("A data store source names SourceDataStoreName, SourceDataStorePath and SourceContainerName together");

        RuleFor(x => x)
            .Must(r => !string.IsNullOrWhiteSpace(r.DestinationDataSet) ^ !string.IsNullOrWhiteSpace(r.DestinationConnectionName))
            .WithMessage("Exactly one of DestinationDataSet or DestinationConnectionName must be supplied");

        When(x => !string.IsNullOrEmpty(x.Description), () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Description cannot exceed 500 characters");
        });
    }
}

/// <summary>
/// Validator for <see cref="UpdatePipelineRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdatePipelineRequestValidator : Validator<UpdatePipelineRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdatePipelineRequestValidator"/> class.
    /// </summary>
    public UpdatePipelineRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Pipeline name is required in the route");

        // Why: PUT with no updateable fields is a no-op. Require at least one. The members and the message come from
        // UpdatePipelineChanges, the one list of what an update may change (FDW-1146); the error code lets the check of a saved
        // pipeline, which accepts a body that changes nothing, tell this failure from the others.
        RuleFor(x => x)
            .Must(req => UpdatePipelineChanges.Stated(req))
            .WithErrorCode(UpdatePipelineChanges.NothingStatedCode)
            .WithMessage(UpdatePipelineChanges.RefusalMessage);

        // Why: the engine binds each side to EXACTLY ONE of {DataSet, ConnectionName}; naming both
        // for one side in a single update has no meaning.
        RuleFor(x => x)
            .Must(r => PipelineSourceRules.SourceForms(r.SourceDataSet, r.SourceConnectionName, r.SourceDataStoreName) <= 1)
            .WithMessage("Supply at most one of SourceDataSet, SourceConnectionName or a data store source");

        RuleFor(x => x)
            .Must(r => (r.SourceDataStoreName is null && r.SourceDataStorePath is null && r.SourceContainerName is null)
                || PipelineSourceRules.DataStoreSourceIsWhole(r.SourceDataStoreName, r.SourceDataStorePath, r.SourceContainerName))
            .WithMessage("A data store source names SourceDataStoreName, SourceDataStorePath and SourceContainerName together");

        RuleFor(x => x)
            .Must(r => r.DestinationDataSet is null || r.DestinationConnectionName is null)
            .WithMessage("Supply at most one of DestinationDataSet or DestinationConnectionName");

        When(x => !string.IsNullOrEmpty(x.Description), () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Description cannot exceed 500 characters");
        });
    }
}

/// <summary>
/// Validator for <see cref="GetPipelineExecutionRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetPipelineExecutionRequestValidator : Validator<GetPipelineExecutionRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetPipelineExecutionRequestValidator"/> class.
    /// </summary>
    public GetPipelineExecutionRequestValidator()
    {
        RuleFor(x => x.ExecutionId)
            .NotEmpty()
            .WithMessage("Execution ID is required");
    }
}
