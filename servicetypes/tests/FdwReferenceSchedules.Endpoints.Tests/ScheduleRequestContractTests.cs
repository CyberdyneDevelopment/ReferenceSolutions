using System.Reflection;
using FdwReferenceProxy.Endpoints;
using Fdw.Services.Scheduling.Clients.Abstractions;

namespace FdwReferenceSchedules.Endpoints.Tests;

/// <summary>
/// FDW-1170: the API host forwards what its proxy request states to the scheduler through the client request. A member the proxy
/// request lacks never reaches the scheduler, so a time zone the console stated on create was dropped without a word. These
/// tests keep the proxy requests and the client requests carrying the same members.
/// </summary>
public sealed class ScheduleRequestContractTests
{
    private static List<string> Members(Type type, params string[] except) => type
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Select(property => property.Name)
        .Where(name => !except.Contains(name, StringComparer.Ordinal))
        .ToList();

    // The proxy resolves the pipeline's name itself (from its id, in the caller's view), so the name is not on its request.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void TheCreateProxyRequestCarriesEveryMemberTheClientRequestSends()
    {
        Members(typeof(ProxyCreateScheduleRequest)).ShouldBe(
            Members(typeof(CreateScheduleClientRequest), nameof(CreateScheduleClientRequest.PipelineName)), ignoreOrder: true);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void TheUpdateProxyRequestCarriesEveryMemberTheClientRequestSends()
    {
        Members(typeof(ProxyUpdateScheduleRequest), nameof(ProxyUpdateScheduleRequest.Name)).ShouldBe(
            Members(typeof(UpdateScheduleClientRequest), nameof(UpdateScheduleClientRequest.PipelineName)), ignoreOrder: true);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void TheCreateProxyRequestCarriesTheTimeZone()
    {
        typeof(ProxyCreateScheduleRequest).GetProperty(nameof(ProxyCreateScheduleRequest.TimeZoneId)).ShouldNotBeNull();
    }
}
