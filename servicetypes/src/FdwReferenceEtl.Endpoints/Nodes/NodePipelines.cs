using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Abstractions.TypeCollections;
using Fdw.Services.Pipelines.Abstractions;
using FdwReferenceEtl.Endpoints.Nodes.Logging;
using Microsoft.Extensions.Logging;

namespace FdwReferenceEtl.Endpoints.Nodes;

/// <summary>
/// Gives a node the pipelines and prerequisites a request names, by pipeline name, as the other endpoints name pipelines.
/// </summary>
/// <remarks>
/// A list the request leaves out is left as the node has it; a list it sends replaces the node's. Nothing is guessed: a name that
/// is not a pipeline, a name sent twice, or pipelines sent to a node that cannot hold them is refused with the reason. Whether the
/// resulting prerequisites make a runnable graph is the node validator's rule, applied when the node is saved.
/// </remarks>
[ExcludeFromCodeCoverage]
internal static class NodePipelines
{
    /// <summary>Replaces the node's pipelines and prerequisites with the ones the request names.</summary>
    /// <param name="pipelines">Finds a pipeline by its name.</param>
    /// <param name="node">The node being saved.</param>
    /// <param name="members">The pipelines the node should hold, or null to leave them as they are.</param>
    /// <param name="prerequisites">The prerequisites between them, or null to leave them as they are.</param>
    /// <param name="logger">The logger that records a refusal.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>Success, or the refusal that names what was wrong.</returns>
    internal static async Task<IGenericResult> Apply(
        IPipelineConfigurationProvider pipelines,
        OrchestrationNodeImplementationConfiguration node,
        IReadOnlyList<NodePipelineRequest>? members,
        IReadOnlyList<NodePrerequisiteRequest>? prerequisites,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (members is null && prerequisites is null)
        {
            return GenericResult.Success();
        }

        if (!OrchestrationNodeTypes.ById(node.NodeTypeId).CanHostPipelines)
        {
            return GenericResult.Failure(
                NodePipelineLog.PipelinesNotAllowed(logger, node.Name, OrchestrationNodeTypes.ById(node.NodeTypeId).Name));
        }

        if (members is not null)
        {
            var memberships = await SetMemberships(pipelines, node, members, logger, cancellationToken).ConfigureAwait(false);
            if (memberships.IsFailure)
            {
                return memberships;
            }
        }

        return prerequisites is null
            ? GenericResult.Success()
            : await SetPrerequisites(pipelines, node, prerequisites, logger, cancellationToken).ConfigureAwait(false);
    }

    // Replaces the node's pipelines; the node is changed only when every name resolved.
    private static async Task<IGenericResult> SetMemberships(
        IPipelineConfigurationProvider pipelines,
        OrchestrationNodeImplementationConfiguration node,
        IReadOnlyList<NodePipelineRequest> members,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var named = new HashSet<string>(StringComparer.Ordinal);
        var memberships = new List<OrchestrationNodePipelineMembershipConfiguration>(members.Count);
        foreach (var member in members)
        {
            if (!named.Add(member.Pipeline))
            {
                return GenericResult.Failure(NodePipelineLog.PipelineRepeated(logger, node.Name, member.Pipeline));
            }

            var id = await IdOf(pipelines, node.Name, member.Pipeline, logger, cancellationToken).ConfigureAwait(false);
            if (id.IsFailure)
            {
                return id;
            }

            memberships.Add(new OrchestrationNodePipelineMembershipConfiguration
            {
                Id = Guid.CreateVersion7(),
                Name = member.Pipeline,
                PipelineId = id.Value,
                Ordinal = member.Ordinal
            });
        }

        node.PipelineMemberships = memberships;
        return GenericResult.Success();
    }

    // Replaces the node's prerequisites; the node is changed only when every name resolved.
    private static async Task<IGenericResult> SetPrerequisites(
        IPipelineConfigurationProvider pipelines,
        OrchestrationNodeImplementationConfiguration node,
        IReadOnlyList<NodePrerequisiteRequest> prerequisites,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var needs = new List<OrchestrationNodePipelinePrerequisiteConfiguration>(prerequisites.Count);
        foreach (var prerequisite in prerequisites)
        {
            var pipeline = await IdOf(pipelines, node.Name, prerequisite.Pipeline, logger, cancellationToken).ConfigureAwait(false);
            if (pipeline.IsFailure)
            {
                return pipeline;
            }

            var first = await IdOf(pipelines, node.Name, prerequisite.Needs, logger, cancellationToken).ConfigureAwait(false);
            if (first.IsFailure)
            {
                return first;
            }

            needs.Add(new OrchestrationNodePipelinePrerequisiteConfiguration
            {
                Id = Guid.CreateVersion7(),
                Name = $"{prerequisite.Pipeline} needs {prerequisite.Needs}",
                PipelineId = pipeline.Value,
                PrerequisitePipelineId = first.Value
            });
        }

        node.PipelinePrerequisites = needs;
        return GenericResult.Success();
    }

    private static async Task<IGenericResult<Guid>> IdOf(
        IPipelineConfigurationProvider pipelines,
        string nodeName,
        string pipelineName,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var found = await pipelines.Get(pipelineName, cancellationToken).ConfigureAwait(false);
        return found.IsSuccess && found.Value is { } pipeline
            ? GenericResult<Guid>.Success(pipeline.Id)
            : GenericResult<Guid>.Failure(NodePipelineLog.PipelineNotFound(logger, nodeName, pipelineName));
    }
}
