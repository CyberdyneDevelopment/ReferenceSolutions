using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Services.Notifications.Abstractions;
using ReferenceNotifications.Webhook;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Shouldly;
using Xunit;
using Fdw;
using Fdw.Services;
using Fdw.Services.Notifications;
using Fdw.Services.Notifications.Webhook;
using Fdw.Services.Notifications.Webhook.Commands;

namespace ReferenceNotifications.Webhook.Tests;

/// <summary>
/// Tests for WebhookNotificationService behavior.
/// </summary>
public sealed class WebhookNotificationServiceTests
{
    private static WebhookNotificationConfiguration BuildConfig(
        string? url = "https://hooks.example.com/notify",
        string? payloadTemplate = null) =>
        new()
        {
            Url = url,
            Method = "POST",
            ContentType = "application/json",
            TimeoutSeconds = 30,
            PayloadTemplate = payloadTemplate
        };

    private static NotificationRequest BuildRequest(string message = "Test message") =>
        new(
            channelName: "Webhook",
            recipients: new[] { "https://hooks.example.com" },
            subject: "Test Subject",
            message: message);

    private static (WebhookNotificationService service, Mock<HttpMessageHandler> handler) BuildServiceWithHandler(
        WebhookNotificationConfiguration config,
        HttpStatusCode responseCode = HttpStatusCode.OK)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(responseCode)
            {
                Content = new StringContent("{\"ok\": true}")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        var logger = NullLogger<WebhookNotificationService>.Instance;
        var service = new WebhookNotificationService(logger, httpClientFactoryMock.Object, config);

        return (service, handlerMock);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ServiceTypeIsWebhook()
    {
        // Arrange
        var config = BuildConfig();
        var (service, _) = BuildServiceWithHandler(config);

        // Assert
        service.ServiceType.ShouldBe("Webhook");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void IsAvailableWhenUrlIsConfigured()
    {
        // Arrange
        var config = BuildConfig(url: "https://hooks.example.com");
        var (service, _) = BuildServiceWithHandler(config);

        // Assert
        service.IsAvailable.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void IsNotAvailableWhenUrlIsNull()
    {
        // Arrange
        var config = BuildConfig(url: null);
        var (service, _) = BuildServiceWithHandler(config);

        // Assert
        service.IsAvailable.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void IsNotAvailableWhenUrlIsEmpty()
    {
        // Arrange
        var config = BuildConfig(url: string.Empty);
        var (service, _) = BuildServiceWithHandler(config);

        // Assert
        service.IsAvailable.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ChannelIsWebhookChannel()
    {
        // Arrange
        var config = BuildConfig();
        var (service, _) = BuildServiceWithHandler(config);

        // Assert
        service.Channel.ShouldNotBeNull();
        service.Channel.Name.ShouldBe("Webhook");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void IdIsNonEmptyGuid()
    {
        // Arrange
        var config = BuildConfig();
        var (service, _) = BuildServiceWithHandler(config);

        // Assert
        service.Id.ShouldNotBeNullOrEmpty();
        Guid.TryParse(service.Id, out _).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ValidateReturnsFailureWhenUrlIsNotConfigured()
    {
        // Arrange
        var config = BuildConfig(url: null);
        var (service, _) = BuildServiceWithHandler(config);
        var request = BuildRequest();

        // Act
        var result = service.Validate(request);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ValidateReturnsFailureWhenMessageIsEmpty()
    {
        // Arrange
        var config = BuildConfig();
        var (service, _) = BuildServiceWithHandler(config);
        var request = BuildRequest(message: string.Empty);

        // Act
        var result = service.Validate(request);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ValidateReturnsFailureWhenUrlIsInvalid()
    {
        // Arrange
        var config = BuildConfig(url: "not-a-valid-url");
        var (service, _) = BuildServiceWithHandler(config);
        var request = BuildRequest();

        // Act
        var result = service.Validate(request);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ValidateReturnsSuccessForValidRequest()
    {
        // Arrange
        var config = BuildConfig();
        var (service, _) = BuildServiceWithHandler(config);
        var request = BuildRequest();

        // Act
        var result = service.Validate(request);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task SendPostsHttpRequestToConfiguredUrl()
    {
        // Arrange
        var config = BuildConfig();
        var (service, handlerMock) = BuildServiceWithHandler(config);
        var request = BuildRequest();

        // Act
        var result = await service.Send(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString() == "https://hooks.example.com/notify"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task SendReturnsSuccessForOkResponse()
    {
        // Arrange
        var config = BuildConfig();
        var (service, _) = BuildServiceWithHandler(config, HttpStatusCode.OK);
        var request = BuildRequest();

        // Act
        var result = await service.Send(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task SendReturnsFailedNotificationResultForBadResponse()
    {
        // Arrange
        var config = BuildConfig();
        var (service, _) = BuildServiceWithHandler(config, HttpStatusCode.InternalServerError);
        var request = BuildRequest();

        // Act
        var result = await service.Send(request, TestContext.Current.CancellationToken);

        // Assert
        // The outer GenericResult is Success (Send doesn't throw), but the notification result indicates failure
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task SendHandlesHttpExceptionGracefully()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handlerMock.Object));

        var config = BuildConfig();
        var service = new WebhookNotificationService(
            NullLogger<WebhookNotificationService>.Instance,
            httpClientFactoryMock.Object,
            config);

        var request = BuildRequest();

        // Act
        var result = await service.Send(request, TestContext.Current.CancellationToken);

        // Assert - Exception is caught and returned as failure, not rethrown
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task SendWithPayloadTemplateReplacesTokens()
    {
        // Arrange
        const string template = "{\"text\": \"{subject}: {body}\", \"type\": \"{type}\"}";
        var config = BuildConfig(payloadTemplate: template);
        HttpRequestMessage? capturedRequest = null;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                capturedRequest = req;
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handlerMock.Object));

        var service = new WebhookNotificationService(
            NullLogger<WebhookNotificationService>.Instance,
            httpClientFactoryMock.Object,
            config);

        var request = BuildRequest("Hello World");

        // Act
        await service.Send(request, TestContext.Current.CancellationToken);

        // Assert - payload should have tokens replaced
        capturedRequest.ShouldNotBeNull();
        var body = await capturedRequest!.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("Test Subject");
        body.ShouldContain("Hello World");
        body.ShouldNotContain("{subject}");
        body.ShouldNotContain("{body}");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task SendWithoutPayloadTemplateUsesDefaultJsonPayload()
    {
        // Arrange
        var config = BuildConfig(payloadTemplate: null);
        HttpRequestMessage? capturedRequest = null;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                capturedRequest = req;
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handlerMock.Object));

        var service = new WebhookNotificationService(
            NullLogger<WebhookNotificationService>.Instance,
            httpClientFactoryMock.Object,
            config);

        var request = BuildRequest();

        // Act
        await service.Send(request, TestContext.Current.CancellationToken);

        // Assert - payload should be standard JSON
        capturedRequest.ShouldNotBeNull();
        var body = await capturedRequest!.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("\"subject\"");
        body.ShouldContain("\"body\"");
        body.ShouldContain("\"type\"");
        body.ShouldContain("\"timestamp\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ExecuteTypedReturnsFailureForUnsupportedCommand()
    {
        // Arrange
        var config = BuildConfig();
        var (service, _) = BuildServiceWithHandler(config);
        var unsupportedCommand = new Mock<IGenericCommand>().Object;

        // Act
        var result = await service.Execute<INotificationResult>(unsupportedCommand, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ExecuteNonGenericReturnsFailureForUnsupportedCommand()
    {
        // Arrange
        var config = BuildConfig();
        var (service, _) = BuildServiceWithHandler(config);
        var unsupportedCommand = new Mock<IGenericCommand>().Object;

        // Act
        var result = await service.Execute(unsupportedCommand, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ExecuteNonGenericReturnsSuccessForValidRequest()
    {
        // Arrange
        var config = BuildConfig();
        var (service, _) = BuildServiceWithHandler(config);
        var request = BuildRequest();

        // Act
        var result = await service.Execute(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConstructorThrowsWhenLoggerIsNull()
    {
        // Arrange
        var config = BuildConfig();
        var httpClientFactory = new Mock<IHttpClientFactory>().Object;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new WebhookNotificationService(null!, httpClientFactory, config));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConstructorThrowsWhenHttpClientFactoryIsNull()
    {
        // Arrange
        var config = BuildConfig();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new WebhookNotificationService(
                NullLogger<WebhookNotificationService>.Instance,
                null!,
                config));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConstructorThrowsWhenConfigurationIsNull()
    {
        // Arrange
        var httpClientFactory = new Mock<IHttpClientFactory>().Object;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new WebhookNotificationService(
                NullLogger<WebhookNotificationService>.Instance,
                httpClientFactory,
                null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DisposeDoesNotThrow()
    {
        // Arrange
        var config = BuildConfig();
        var (service, _) = BuildServiceWithHandler(config);

        // Act & Assert
        Should.NotThrow(() => service.Dispose());
    }
}
