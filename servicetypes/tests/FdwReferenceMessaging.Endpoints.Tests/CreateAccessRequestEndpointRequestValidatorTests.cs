using System.Linq;
using FdwReferenceMessages.Endpoints;
using Fdw.Services.Messaging.Endpoints.Models;
using Shouldly;
using Xunit;

namespace FdwReferenceMessaging.Endpoints.Tests;

/// <summary>
/// FDW-2863: an access request that names no resource or no permission is refused with the field named, where it was stored
/// with both empty.
/// </summary>
public class CreateAccessRequestEndpointRequestValidatorTests
{
    private static readonly CreateAccessRequestEndpointRequestValidator Validator = new();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AnEmptyBodyIsRefusedNamingBothFields()
    {
        var errors = Validator.Validate(new CreateAccessRequestEndpointRequest()).Errors;

        errors.Select(e => e.PropertyName).ShouldBe(
            [nameof(CreateAccessRequestEndpointRequest.RequestedResource), nameof(CreateAccessRequestEndpointRequest.RequestedPermission)],
            ignoreOrder: true);
        errors.Select(e => e.ErrorMessage).ShouldContain("RequestedResource is required");
        errors.Select(e => e.ErrorMessage).ShouldContain("RequestedPermission is required");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AResourceOfNothingOrOnlySpacesIsRefused(string resource)
    {
        var request = Request();
        request.RequestedResource = resource;

        Validator.Validate(request).Errors.ShouldHaveSingleItem().PropertyName.ShouldBe(nameof(CreateAccessRequestEndpointRequest.RequestedResource));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void APermissionOfNothingOrOnlySpacesIsRefused(string permission)
    {
        var request = Request();
        request.RequestedPermission = permission;

        Validator.Validate(request).Errors.ShouldHaveSingleItem().PropertyName.ShouldBe(nameof(CreateAccessRequestEndpointRequest.RequestedPermission));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ARequestThatNamesAResourceAndAPermissionIsAcceptedWithoutAJustificationOrAReference()
    {
        var request = Request();

        request.Justification.ShouldBeNull();
        request.ReferenceId.ShouldBeNull();
        Validator.Validate(request).IsValid.ShouldBeTrue();
    }

    private static CreateAccessRequestEndpointRequest Request() => new()
    {
        RequestedResource = "datasets",
        RequestedPermission = "datasets:read",
    };
}
