using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Abstractions.TypeCollections;
using Microsoft.Extensions.Logging;
using Reference.Api.Logging;

namespace Reference.Api.Endpoints.Projects;

/// <summary>
/// Maps OrchestrationNodeConfiguration trees to the ProjectConfiguration client contract
/// (IProjectApiClient shape). Returns null and logs structured errors on unknown NodeTypeId.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class OrchestrationNodeMapper
{
    /// <summary>
    /// Maps a project-type node and its recursive Children to ProjectConfiguration.
    /// Returns null if any child carries an unrecognized NodeTypeId (error is logged).
    /// </summary>
    internal static ProjectConfiguration? ToProject(OrchestrationNodeConfiguration node, ILogger logger)
    {
        var stages = new List<StageConfiguration>(node.Children.Count);
        foreach (var child in node.Children.OrderBy(c => c.Ordinal))
        {
            if (child.NodeTypeId != OrchestrationNodeTypes.Stage.Id)
            {
                ProjectLog.ProjectUnknownChildNodeType(logger, node.Id, child.NodeTypeId, child.Id);
                return null;
            }
            var stage = ToStage(child, node.Id, logger);
            if (stage is null)
                return null;
            stages.Add(stage);
        }

        return new ProjectConfiguration
        {
            Id = node.Id,
            Name = node.Name,
            Description = node.Description,
            IsEnabled = node.IsEnabled,
            TenantId = node.TenantId,
            VisibilityGroupId = node.VisibilityGroupId,
            StepFailurePolicy = node.StepFailurePolicy,
            StageFailurePolicy = node.StageFailurePolicy,
            MaxParallelPipelines = node.MaxParallelPipelines,
            RequireApprovalToRun = node.RequireApprovalToRun,
            AllowResume = node.AllowResume,
            AllowCrossTenant = node.AllowCrossTenant,
            ResiliencyPolicyId = node.ResiliencyPolicyId,
            Stages = stages,
        };
    }

    private static StageConfiguration? ToStage(OrchestrationNodeConfiguration stageNode, Guid projectId, ILogger logger)
    {
        var steps = new List<StepConfiguration>(stageNode.Children.Count);
        foreach (var child in stageNode.Children.OrderBy(c => c.Ordinal))
        {
            if (child.NodeTypeId != OrchestrationNodeTypes.Step.Id)
            {
                ProjectLog.ProjectUnknownChildNodeType(logger, stageNode.Id, child.NodeTypeId, child.Id);
                return null;
            }
            steps.Add(ToStep(child, stageNode.Id));
        }

        return new StageConfiguration
        {
            Id = stageNode.Id,
            Name = stageNode.Name,
            ProjectConfigurationId = projectId,
            Ordinal = stageNode.Ordinal,
            StepFailurePolicy = stageNode.StepFailurePolicy,
            StageFailurePolicy = stageNode.StageFailurePolicy,
            MaxParallelPipelines = stageNode.MaxParallelPipelines,
            RequireApprovalToRun = stageNode.RequireApprovalToRun,
            AllowResume = stageNode.AllowResume,
            AllowCrossTenant = stageNode.AllowCrossTenant,
            ResiliencyPolicyId = stageNode.ResiliencyPolicyId,
            Steps = steps,
        };
    }

    private static StepConfiguration ToStep(OrchestrationNodeConfiguration stepNode, Guid stageId)
    {
        return new StepConfiguration
        {
            Id = stepNode.Id,
            Name = stepNode.Name,
            ProjectStageConfigurationId = stageId,
            Ordinal = stepNode.Ordinal,
            StepFailurePolicy = stepNode.StepFailurePolicy,
            StageFailurePolicy = stepNode.StageFailurePolicy,
            MaxParallelPipelines = stepNode.MaxParallelPipelines,
            RequireApprovalToRun = stepNode.RequireApprovalToRun,
            AllowResume = stepNode.AllowResume,
            AllowCrossTenant = stepNode.AllowCrossTenant,
            ResiliencyPolicyId = stepNode.ResiliencyPolicyId,
            Pipelines = stepNode.PipelineMemberships
                .OrderBy(m => m.Ordinal)
                .Select(m => new StepPipelineMembershipConfiguration
                {
                    Id = m.Id,
                    Name = m.Name,
                    StageStepConfigurationId = stepNode.Id,
                    PipelineId = m.PipelineId,
                    Ordinal = m.Ordinal,
                }).ToList(),
            Prerequisites = stepNode.PipelinePrerequisites
                .Select(p => new StepPipelinePrerequisiteConfiguration
                {
                    Id = p.Id,
                    Name = p.Name,
                    StageStepConfigurationId = stepNode.Id,
                    PipelineId = p.PipelineId,
                    PrerequisitePipelineId = p.PrerequisitePipelineId,
                }).ToList(),
        };
    }
}
