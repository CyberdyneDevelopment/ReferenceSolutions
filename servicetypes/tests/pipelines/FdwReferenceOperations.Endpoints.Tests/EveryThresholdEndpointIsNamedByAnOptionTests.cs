using FastEndpoints;
using Fdw.Operations.Endpoints.Thresholds;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceThresholds.Endpoints;

namespace FdwReferenceOperations.Endpoints.Tests;

/// <summary>
/// FDW-3949: a host exposes an endpoint only through an option of its endpoint collection (FDW-3097). The threshold endpoints are checked both ways: every concrete threshold
/// endpoint class is named by an option, and every threshold endpoint base the platform defines has a concrete class here, so a base added to FDW without its host class (or a class
/// left without its option) fails the build and not the deploy.
/// </summary>
public sealed class EveryThresholdEndpointIsNamedByAnOptionTests
{
    private static readonly Type[] HostTypes = typeof(GetThresholdTypesEndpoint).Assembly.GetTypes();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Registration")]
    public void EveryConcreteThresholdEndpointIsNamedByAnOption()
    {
        var named = HostTypes
            .Where(t => !t.IsAbstract && typeof(EndpointTypeOptionBase).IsAssignableFrom(t))
            .Select(t => ((EndpointTypeOptionBase)Activator.CreateInstance(t)!).EndpointType)
            .ToHashSet();

        var unrouted = HostTypes
            .Where(t => t.IsPublic && !t.IsAbstract && t.Namespace == typeof(GetThresholdTypesEndpoint).Namespace && typeof(BaseEndpoint).IsAssignableFrom(t) && !named.Contains(t))
            .Select(t => t.FullName)
            .Order(StringComparer.Ordinal)
            .ToList();

        unrouted.ShouldBeEmpty($"No option names: {string.Join(", ", unrouted)}");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Registration")]
    public void EveryThresholdEndpointBaseOfThePlatformHasAConcreteHostEndpoint()
    {
        var bases = typeof(CreateThresholdEndpointBase).Assembly.GetTypes()
            .Where(t => t.IsPublic && t.IsAbstract && t.Namespace == typeof(CreateThresholdEndpointBase).Namespace && typeof(BaseEndpoint).IsAssignableFrom(t))
            .ToList();
        bases.ShouldNotBeEmpty();

        var withoutAHost = bases
            .Where(b => !HostTypes.Any(t => !t.IsAbstract && b.IsAssignableFrom(t)))
            .Select(b => b.FullName)
            .Order(StringComparer.Ordinal)
            .ToList();

        withoutAHost.ShouldBeEmpty($"No concrete host endpoint derives: {string.Join(", ", withoutAHost)}");
    }
}
