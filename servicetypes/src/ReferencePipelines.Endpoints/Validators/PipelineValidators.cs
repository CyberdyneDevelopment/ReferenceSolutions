using System.Diagnostics.CodeAnalysis;
using FastEndpoints;
using FluentValidation;
using Fdw.Services.Pipelines.Endpoints;

namespace ReferencePipelines.Endpoints.Validators;

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
        RuleFor(x => x)
            .Must(r => !string.IsNullOrWhiteSpace(r.SourceDataSet) ^ !string.IsNullOrWhiteSpace(r.SourceConnectionName))
            .WithMessage("Exactly one of SourceDataSet or SourceConnectionName must be supplied");

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

        // Why: PUT with no updateable fields is a no-op. Require at least one.
        RuleFor(x => x)
            .Must(req => req.SourceConnectionName is not null
                || req.DestinationConnectionName is not null
                || req.SourceDataSet is not null
                || req.DestinationDataSet is not null
                || req.Description is not null
                || req.IsEnabled.HasValue)
            .WithMessage("At least one updateable field must be supplied (SourceConnectionName, DestinationConnectionName, SourceDataSet, DestinationDataSet, Description, or IsEnabled)");

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
