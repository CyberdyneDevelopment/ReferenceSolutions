using FastEndpoints;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferencePipelines.Endpoints;

namespace FdwReferenceEtl.Endpoints.Tests;

/// <summary>
/// FDW-3097: a host exposes an endpoint only through an option of its endpoint collection (PipelineEndpoints and the others). An endpoint
/// class that no option names builds, deploys and is never mapped: the API answers 405, as for a path that does not exist, and nothing else
/// reports it. This pins that every concrete endpoint class of the ETL endpoints is named by an option.
/// </summary>
public sealed class EveryEndpointIsNamedByAnOptionTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Registration")]
    public void EveryConcreteEndpointClassIsNamedByAnOptionOfAnEndpointCollection()
    {
        var types = typeof(ListPipelineRunQueueEndpoint).Assembly.GetTypes();
        var named = types
            .Where(t => !t.IsAbstract && typeof(EndpointTypeOptionBase).IsAssignableFrom(t))
            .Select(t => ((EndpointTypeOptionBase)Activator.CreateInstance(t)!).EndpointType)
            .ToHashSet();

        var unrouted = types
            .Where(t => t.IsPublic && !t.IsAbstract && typeof(BaseEndpoint).IsAssignableFrom(t) && !named.Contains(t))
            .Select(t => t.FullName)
            .Order(StringComparer.Ordinal)
            .ToList();

        unrouted.ShouldBeEmpty($"No option names: {string.Join(", ", unrouted)}");
    }
}
