using FastEndpoints;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceProxy.Endpoints;

namespace FdwReferenceSchedules.Endpoints.Tests;

/// <summary>
/// FDW-3097: reference-api exposes a proxy endpoint only through an option of ProxyEndpoints. A proxy class that no option names builds,
/// deploys and is never mapped: the API answers 405, as for a path that does not exist, and nothing else reports it. This pins that every
/// concrete endpoint class of the proxy endpoints is named by an option.
/// </summary>
public sealed class EveryProxyEndpointIsNamedByAnOptionTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Registration")]
    public void EveryConcreteProxyEndpointClassIsNamedByAnOptionOfProxyEndpoints()
    {
        var types = typeof(ListPipelineRunQueueProxyEndpoint).Assembly.GetTypes();
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
