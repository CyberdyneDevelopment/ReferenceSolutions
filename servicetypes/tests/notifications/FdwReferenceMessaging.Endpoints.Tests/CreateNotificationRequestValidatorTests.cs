using FdwReferenceNotifications.Endpoints;
using Shouldly;
using Xunit;

namespace FdwReferenceMessaging.Endpoints.Tests;

/// <summary>
/// A notification is created enabled or disabled as the caller says (FDW-1180); one that leaves <c>IsEnabled</c> out takes the caller's
/// tenant's setting DefaultNotificationChannelIsEnabled when it is made, so the validator does not refuse it (FDW-3580).
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
    public void ALeftOutIsEnabledIsAcceptedForItsSettingToGive()
    {
        Validator.Validate(Request(null)).Errors.ShouldNotContain(e => e.PropertyName == nameof(CreateNotificationRequest.IsEnabled));
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
