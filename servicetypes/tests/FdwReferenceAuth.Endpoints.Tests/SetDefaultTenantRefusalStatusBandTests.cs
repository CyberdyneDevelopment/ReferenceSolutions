using System.Reflection;
using Fdw.MessageLogging;
using FdwReferenceAuth.Endpoints.Logging;
using Shouldly;
using Xunit;

namespace FdwReferenceAuth.Endpoints.Tests;

/// <summary>
/// FDW-2892: a person who asks to make a tenant their default and may not is answered 403 with the reason. The status comes
/// from the category of the refusal's event id (the number divided by 10000), and 403 is category 10, so both refusals must
/// be six-digit ids that start with 10. An id below 10000 has no category, and the answer would be a bare 500.
/// </summary>
public class SetDefaultTenantRefusalStatusBandTests
{
    private const int ForbiddenCategory = 10;

    [Theory]
    [InlineData(nameof(TenantLog.SetDefaultTenantNotMember))]
    [InlineData(nameof(TenantLog.SetDefaultTenantNotSelectable))]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ARefusalToSetADefaultTenantIsInTheForbiddenBandAndStatesAReasonForTheCaller(string method)
    {
        var refusal = typeof(TenantLog).GetMethod(method, BindingFlags.Public | BindingFlags.Static);

        refusal.ShouldNotBeNull();
        var attribute = refusal.GetCustomAttribute<MessageLoggingAttribute>();
        attribute.ShouldNotBeNull();
        (attribute.EventId / 10000).ShouldBe(ForbiddenCategory);
        attribute.ClientMessage.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TheNotAMemberReasonDoesNotSayWhetherTheTenantExists()
    {
        var refusal = typeof(TenantLog).GetMethod(nameof(TenantLog.SetDefaultTenantNotMember), BindingFlags.Public | BindingFlags.Static);

        refusal.ShouldNotBeNull();
        var reason = refusal.GetCustomAttribute<MessageLoggingAttribute>()?.ClientMessage;
        reason.ShouldNotBeNull();
        reason.ShouldNotContain("exist");
        reason.ShouldNotContain("{");
    }
}
