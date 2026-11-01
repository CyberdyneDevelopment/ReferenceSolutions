using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Etl.Projects.Abstractions.TypeCollections;
using FdwReferenceEtl.Endpoints.Nodes.Logging;
using Microsoft.Extensions.Logging;

namespace FdwReferenceEtl.Endpoints.Nodes;

/// <summary>Works out which node type a create request means, from the name or the number it sends.</summary>
/// <remarks>
/// The name is resolved through <see cref="OrchestrationNodeTypes"/>, the collection everything else asks, so a node type a developer
/// adds works here without a change. Nothing is defaulted: a request that says neither, or says two things that disagree, is refused.
/// </remarks>
[ExcludeFromCodeCoverage]
internal static class NodeTypes
{
    /// <summary>Resolves the node type of a node being created.</summary>
    /// <param name="nodeType">The node type name the request sent, or null.</param>
    /// <param name="nodeTypeId">The node type id the request sent, or null.</param>
    /// <param name="nodeName">The name of the node, for the refusal.</param>
    /// <param name="logger">The logger that records a refusal.</param>
    /// <returns>The node type id, or the refusal that says what was wrong.</returns>
    internal static IGenericResult<int> Resolve(string? nodeType, int? nodeTypeId, string nodeName, ILogger logger)
    {
        var known = string.Join(", ", OrchestrationNodeTypes.All().Select(t => t.Name));
        if (nodeType is null)
        {
            return nodeTypeId is { } number
                ? GenericResult<int>.Success(number)
                : GenericResult<int>.Failure(NodeTypeLog.NodeTypeNotStated(logger, nodeName, known));
        }

        var named = OrchestrationNodeTypes.ByName(nodeType);
        if (ReferenceEquals(named, OrchestrationNodeTypes.NotFound))
        {
            return GenericResult<int>.Failure(NodeTypeLog.NodeTypeUnknown(logger, nodeName, nodeType, known));
        }

        return nodeTypeId is { } stated && stated != named.Id
            ? GenericResult<int>.Failure(NodeTypeLog.NodeTypeDisagrees(logger, nodeName, nodeType, stated))
            : GenericResult<int>.Success(named.Id);
    }
}
