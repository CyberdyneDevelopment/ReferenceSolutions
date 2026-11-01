using System.Reflection;
using Fdw.MessageLogging;
using FdwReferenceAuth.Endpoints.Logging;
using Shouldly;
using Xunit;

namespace FdwReferenceAuth.Endpoints.Tests;

/// <summary>
/// FDW-2938: a person who asks to make a department their default and may not is answered 403 with the reason, the same answer whether the
/// department does not exist, belongs to another company, is deleted or is one they hold no access in. The status comes from the category of
/// the refusal's event id (the number divided by 10000), and 403 is category 10, so both refusals must be six-digit ids that start with 10.
/// </summary>
public class SetDefaultOrganizationRefusalStatusBandTests
{
    private const int ForbiddenCategory = 10;

    private static MessageLoggingAttribute Attribute(string method)
    {
        var refusal = typeof(TenantLog).GetMethod(method, BindingFlags.Public | BindingFlags.Static);
        refusal.ShouldNotBeNull();
        var attribute = refusal.GetCustomAttribute<MessageLoggingAttribute>();
        attribute.ShouldNotBeNull();
        return attribute;
    }

    [Theory]
    [InlineData(nameof(TenantLog.SetDefaultOrganizationNotHeld))]
    [InlineData(nameof(TenantLog.DefaultOrganizationNoMembership))]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ARefusalToKeepADefaultOrganizationIsInTheForbiddenBandAndStatesAReasonForTheCaller(string method)
    {
        var attribute = Attribute(method);

        (attribute.EventId / 10000).ShouldBe(ForbiddenCategory);
        attribute.ClientMessage.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void TheNotHeldReasonDoesNotSayWhetherTheDepartmentExistsOrNamesIt()
    {
        var reason = Attribute(nameof(TenantLog.SetDefaultOrganizationNotHeld)).ClientMessage;

        reason.ShouldNotBeNull();
        reason.ShouldNotContain("exist");
        reason.ShouldNotContain("{");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ASaveThatFailsAnswersAsADependencyFailure()
    {
        (Attribute(nameof(TenantLog.DefaultOrganizationNotSaved)).EventId / 10000).ShouldBe(7);
    }
}
