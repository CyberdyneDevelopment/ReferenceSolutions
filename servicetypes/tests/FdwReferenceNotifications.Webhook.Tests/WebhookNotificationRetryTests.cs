using System.Net;
using Fdw.Results;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceNotifications.Webhook.Tests;

/// <summary>
/// FDW-1913: a Webhook notification service tries a delivery again, up to its retry count, when the receiver cannot be reached, does not
/// answer in time or answers with a server error, stops at the first success, and does not repeat a refusal.
/// </summary>
public sealed class WebhookNotificationRetryTests
{
    private const string SecretValueText = "s3cr3t-value-for-the-retry-test";

    private static WebhookNotificationConfiguration Config(int retryCount) => new()
    {
        Url = "https://hooks.example.com/notify",
        Method = "POST",
        ContentType = "application/json",
        TimeoutSeconds = 30,
        RetryCount = retryCount,
    };

    private static NotificationRequest Request() =>
        new(channelName: "Webhook", recipients: new[] { "https://hooks.example.com" }, subject: "Subject", message: "Message");

    private static Func<HttpRequestMessage, HttpResponseMessage> Answers(HttpStatusCode status) => _ => new HttpResponseMessage(status);

    private static Func<HttpRequestMessage, HttpResponseMessage> Fails(Exception exception) => _ => throw exception;

    // The service over a receiver that answers each call with the next of the given answers; the headers it saw are kept per call.
    private static (WebhookNotificationService Service, List<bool> SecretSent) Build(
        WebhookNotificationConfiguration config, Mock<ISecretManagerProvider> secretManagers, params Func<HttpRequestMessage, HttpResponseMessage>[] answers)
    {
        var secretSent = new List<bool>();
        var handler = new ScriptedHandler(answers, secretSent);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler, disposeHandler: false));
        return (new WebhookNotificationService(NullLogger<WebhookNotificationService>.Instance, factory.Object, secretManagers.Object, config), secretSent);
    }

    private static (WebhookNotificationService Service, List<bool> SecretSent) Build(
        WebhookNotificationConfiguration config, params Func<HttpRequestMessage, HttpResponseMessage>[] answers) =>
        Build(config, new Mock<ISecretManagerProvider>(), answers);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AServerErrorIsTriedAgainAndTheFirstSuccessEndsTheDelivery()
    {
        var (service, calls) = Build(Config(retryCount: 2), Answers(HttpStatusCode.ServiceUnavailable), Answers(HttpStatusCode.OK), Answers(HttpStatusCode.OK));

        var result = await service.Send(Request(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.IsSuccess.ShouldBeTrue();
        calls.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ADeliveryThatKeepsFailingIsTriedOnceAndThenRetryCountMoreTimesAndThenFails()
    {
        var (service, calls) = Build(Config(retryCount: 1), Answers(HttpStatusCode.InternalServerError), Answers(HttpStatusCode.BadGateway), Answers(HttpStatusCode.OK));

        var result = await service.Send(Request(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.IsSuccess.ShouldBeFalse();
        calls.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ARefusalIsNotTriedAgain()
    {
        var (service, calls) = Build(Config(retryCount: 3), Answers(HttpStatusCode.BadRequest), Answers(HttpStatusCode.OK));

        var result = await service.Send(Request(), TestContext.Current.CancellationToken);

        result.Value!.IsSuccess.ShouldBeFalse();
        calls.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AReceiverThatCannotBeReachedIsTriedAgain()
    {
        var (service, calls) = Build(Config(retryCount: 1), Fails(new HttpRequestException("Connection refused")), Answers(HttpStatusCode.OK));

        var result = await service.Send(Request(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.IsSuccess.ShouldBeTrue();
        calls.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ANoRetryServiceCallsOnceAndReturnsTheFailure()
    {
        var (service, calls) = Build(Config(retryCount: 0), Fails(new HttpRequestException("Connection refused")), Answers(HttpStatusCode.OK));

        var result = await service.Send(Request(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        calls.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task TheSecretHeaderIsSentOnEveryTry()
    {
        var config = Config(retryCount: 1);
        config.SecretManagerName = "Main";
        config.SecretKeyName = "partner-hook";
        var manager = new Mock<ISecretManager>();
        manager.Setup(m => m.Execute(It.IsAny<ISecretManagerCommand<SecretValue>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<SecretValue>.Success(new SecretValue("partner-hook", SecretValueText)));
        var secretManagers = new Mock<ISecretManagerProvider>();
        secretManagers.Setup(p => p.Get("Main", It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<ISecretManager>.Success(manager.Object));
        var (service, calls) = Build(config, secretManagers, Answers(HttpStatusCode.ServiceUnavailable), Answers(HttpStatusCode.OK));

        var result = await service.Send(Request(), TestContext.Current.CancellationToken);

        result.Value!.IsSuccess.ShouldBeTrue();
        calls.ShouldBe([true, true]);
        secretManagers.Verify(p => p.Get("Main", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ANegativeRetryCountSendsNothing()
    {
        var (service, calls) = Build(Config(retryCount: -1), Answers(HttpStatusCode.OK));

        var result = await service.Send(Request(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        calls.ShouldBeEmpty();
    }

    // Answers each call with the next scripted answer, and keeps whether each call carried the secret header.
    private sealed class ScriptedHandler(Func<HttpRequestMessage, HttpResponseMessage>[] answers, List<bool> secretSent) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            secretSent.Add(request.Headers.Contains(WebhookNotificationService.SecretHeader));
            return Task.FromResult(answers[secretSent.Count - 1](request));
        }
    }
}
