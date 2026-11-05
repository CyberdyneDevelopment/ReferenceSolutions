using Fdw.Services.Connections.Endpoints;
using FdwReferenceConnections.Endpoints.Validators;

namespace FdwReferenceData.Endpoints.Tests;

/// <summary>Tests that the host's create-connection validator runs FDW's rules and adds the service-type rule (FDW-3572).</summary>
public sealed class ReferenceCreateConnectionRequestValidatorTests
{
    [Fact]
    public void AnFdwRuleRefusesThroughTheHostClass()
    {
        var result = new ReferenceCreateConnectionRequestValidator().Validate(new CreateConnectionRequest { Name = "1Orders", ServiceType = "MsSql" });

        result.Errors.ShouldContain(error => error.PropertyName == "Name");
    }

    [Fact]
    public void AServiceTypeTheHostDidNotRegisterIsRefusedWithTheTypesItDid()
    {
        var result = new ReferenceCreateConnectionRequestValidator().Validate(new CreateConnectionRequest { Name = "Orders", ServiceType = "NoSuchType" });

        result.Errors.ShouldContain(error => error.ErrorMessage.StartsWith("Unknown service type 'NoSuchType'. Valid types:", StringComparison.Ordinal));
    }

    [Fact]
    public void AnEmptyServiceTypeIsAnsweredOnce()
    {
        var result = new ReferenceCreateConnectionRequestValidator().Validate(new CreateConnectionRequest { Name = "Orders", ServiceType = string.Empty });

        result.Errors.Count(error => error.PropertyName == "ServiceType").ShouldBe(1);
    }
}
