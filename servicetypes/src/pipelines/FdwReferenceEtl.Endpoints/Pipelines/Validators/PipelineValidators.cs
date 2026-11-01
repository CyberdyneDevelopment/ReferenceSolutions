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
