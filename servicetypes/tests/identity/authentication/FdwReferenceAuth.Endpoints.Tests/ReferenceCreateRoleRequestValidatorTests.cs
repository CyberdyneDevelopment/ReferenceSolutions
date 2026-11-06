using Fdw.Services.Authorization.Endpoints;
using FdwReferenceAuth.Endpoints.Validators;

namespace FdwReferenceAuth.Endpoints.Tests;

/// <summary>Tests that the host's create-role validator runs FDW's rules (FDW-3572).</summary>
public sealed class ReferenceCreateRoleRequestValidatorTests
{
    [Fact]
    public void AValidRoleIsAccepted() =>
        new ReferenceCreateRoleRequestValidator().Validate(new CreateRoleRequest { Name = "data-engineer", IsTenantScoped = false }).IsValid.ShouldBeTrue();

    [Fact]
    public void AnFdwRuleRefusesThroughTheHostClass() =>
        new ReferenceCreateRoleRequestValidator().Validate(new CreateRoleRequest { Name = "ab" }).Errors
            .ShouldContain(error => error.ErrorMessage == "Role name must be at least 3 characters");
}
