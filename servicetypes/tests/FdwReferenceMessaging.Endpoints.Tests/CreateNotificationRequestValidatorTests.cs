using System.Linq;
using FdwReferenceNotifications.Endpoints;
using Shouldly;
using Xunit;

namespace FdwReferenceMessaging.Endpoints.Tests;

/// <summary>
/// A notification is created enabled or disabled because the caller said so (FDW-1180): the request states
/// <c>IsEnabled</c>, and one that leaves it out is refused with the reason.
/// </summary>
public class CreateNotificationRequestValidatorTests
{
    private static readonly CreateNotificationRequestValidator Validator = new();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AStatedIsEnabledIsAccepted(bool isEnabled)
    {
        Validator.Validate(Request(isEnabled)).Errors.ShouldNotContain(e => e.PropertyName == nameof(CreateNotificationRequest.IsEnabled));
    }

    [Fact]
    public void ALeftOutIsEnabledIsRefusedWithTheReason()
    {
        var errors = Validator.Validate(Request(null)).Errors.Where(e => e.PropertyName == nameof(CreateNotificationRequest.IsEnabled)).ToList();

        errors.ShouldHaveSingleItem().ErrorMessage.ShouldContain("IsEnabled is required");
    }

    [Fact]
    public void ANameIsStillRequired()
    {
        var request = Request(true);
        request.Name = string.Empty;

        Validator.Validate(request).Errors.ShouldContain(e => e.PropertyName == nameof(CreateNotificationRequest.Name));
    }

    private static CreateNotificationRequest Request(bool? isEnabled) => new()
    {
        Name = "Console",
        NotificationType = "Console",
        IsEnabled = isEnabled,
    };
}
