using System.Linq;
using FdwReferenceNotifications.Endpoints;
using Shouldly;
using Xunit;

namespace FdwReferenceMessaging.Endpoints.Tests;

/// <summary>
/// A Webhook notification service is stated whole (FDW-1909): each field it needs is stated, a field left out or wrong is refused with its
/// name, and the secret is named by the secret manager and the key together or not at all.
/// </summary>
public class WebhookNotificationRequestValidatorTests
{
    private static readonly WebhookNotificationRequestValidator Validator = new();

    private static WebhookNotificationRequest Complete() => new()
    {
        Name = "PartnerHook",
        IsEnabled = true,
        Url = "https://hooks.example.com/notify",
        Method = "POST",
        ContentType = "application/json",
        RetryCount = 3,
        TimeoutSeconds = 30,
    };

    // Property names compare ignoring case: FastEndpoints sets a process-wide validator name resolver (camelCase) when any test host in this process starts, so the case a name has depends on which classes ran first.
    private static string[] Refused(WebhookNotificationRequest request) =>
        [.. Validator.Validate(request).Errors.Select(error => error.PropertyName.ToLowerInvariant()).Distinct()];

    [Fact]
    public void AServiceStatedWholeIsAccepted()
    {
        Validator.Validate(Complete()).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void AServiceWithAWholeSecretIsAccepted()
    {
        var request = Complete();
        request.SecretManagerName = "Main";
        request.SecretKeyName = "partner-hook";

        Validator.Validate(request).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ALeftOutUrlIsRefusedNamingIt()
    {
        var request = Complete();
        request.Url = null;

        Refused(request).ShouldBe([nameof(WebhookNotificationRequest.Url).ToLowerInvariant()]);
    }

    [Theory]
    [InlineData("hooks.example.com/notify")]
    [InlineData("ftp://hooks.example.com/notify")]
    [InlineData("not an address")]
    public void AUrlThatIsNotAnHttpAddressIsRefused(string url)
    {
        var request = Complete();
        request.Url = url;

        Refused(request).ShouldBe([nameof(WebhookNotificationRequest.Url).ToLowerInvariant()]);
    }

    [Theory]
    [InlineData("http://192.0.2.10:8080/hook")]
    [InlineData("https://hooks.example.com/notify")]
    public void AnHttpOrHttpsAddressIsAccepted(string url)
    {
        var request = Complete();
        request.Url = url;

        Validator.Validate(request).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("GET")]
    [InlineData("DELETE")]
    public void AMethodThatIsNotPostPutOrPatchIsRefused(string method)
    {
        var request = Complete();
        request.Method = method;

        Refused(request).ShouldBe([nameof(WebhookNotificationRequest.Method).ToLowerInvariant()]);
    }

    [Theory]
    [InlineData("post")]
    [InlineData("PUT")]
    [InlineData("Patch")]
    public void AMethodIsAcceptedWhateverItsCase(string method)
    {
        var request = Complete();
        request.Method = method;

        Validator.Validate(request).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ALeftOutContentTypeIsRefusedNamingIt()
    {
        var request = Complete();
        request.ContentType = string.Empty;

        Refused(request).ShouldBe([nameof(WebhookNotificationRequest.ContentType).ToLowerInvariant()]);
    }

    [Fact]
    public void ALeftOutIsEnabledIsRefusedNamingIt()
    {
        var request = Complete();
        request.IsEnabled = null;

        Refused(request).ShouldBe([nameof(WebhookNotificationRequest.IsEnabled).ToLowerInvariant()]);
    }

    [Fact]
    public void ALeftOutOrNegativeRetryCountIsRefused()
    {
        var left = Complete();
        left.RetryCount = null;
        var negative = Complete();
        negative.RetryCount = -1;

        Refused(left).ShouldBe([nameof(WebhookNotificationRequest.RetryCount).ToLowerInvariant()]);
        Refused(negative).ShouldBe([nameof(WebhookNotificationRequest.RetryCount).ToLowerInvariant()]);
    }

    [Fact]
    public void AZeroRetryCountIsAccepted()
    {
        var request = Complete();
        request.RetryCount = 0;

        Validator.Validate(request).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-5)]
    public void ALeftOutOrNotPositiveTimeoutIsRefused(int? seconds)
    {
        var request = Complete();
        request.TimeoutSeconds = seconds;

        Refused(request).ShouldBe([nameof(WebhookNotificationRequest.TimeoutSeconds).ToLowerInvariant()]);
    }

    [Fact]
    public void ASecretManagerWithoutAKeyIsRefusedNamingTheKey()
    {
        var request = Complete();
        request.SecretManagerName = "Main";

        Refused(request).ShouldBe([nameof(WebhookNotificationRequest.SecretKeyName).ToLowerInvariant()]);
    }

    [Fact]
    public void AKeyWithoutASecretManagerIsRefusedNamingTheSecretManager()
    {
        var request = Complete();
        request.SecretKeyName = "partner-hook";

        Refused(request).ShouldBe([nameof(WebhookNotificationRequest.SecretManagerName).ToLowerInvariant()]);
    }

    [Fact]
    public void ANameIsRequired()
    {
        var request = Complete();
        request.Name = string.Empty;

        Refused(request).ShouldBe([nameof(WebhookNotificationRequest.Name).ToLowerInvariant()]);
    }
}
