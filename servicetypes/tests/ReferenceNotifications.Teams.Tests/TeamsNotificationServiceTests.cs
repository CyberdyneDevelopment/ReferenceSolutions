using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Notifications.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceNotifications.Teams;

namespace ReferenceNotifications.Teams.Tests;

/// <summary>
/// Tests for <see cref="TeamsNotificationService"/>: validation branches, the MessageCard/Adaptive
/// Card send paths (success, partial webhook failure, transport exception), the explicit
/// <see cref="IGenericService"/> Execute overloads (which reject all commands with a fixed code —
/// notifications flow only through <see cref="INotificationService.Send"/>), and trivial members.
/// </summary>
public sealed class TeamsNotificationServiceTests
{
    private static TeamsNotificationConfiguration CreateConfiguration(
        string? defaultWebhookUrl = null,
        bool useAdaptiveCards = true,
        int timeoutSeconds = 30) =>
        new()
        {
            DefaultWebhookUrl = defaultWebhookUrl,
            UseAdaptiveCards = useAdaptiveCards,
            TimeoutSeconds = timeoutSeconds,
        };

    private static Mock<IHttpClientFactory> CreateHttpClientFactory(MockHttpMessageHandler handler)
    {
        var mock = new Mock<IHttpClientFactory>();
        mock.Setup(f => f.CreateClient("TeamsNotifications")).Returns(new HttpClient(handler));
        return mock;
    }

    private static TeamsNotificationService CreateService(
        TeamsNotificationConfiguration configuration,
        MockHttpMessageHandler handler) =>
        new(
            NullLogger<TeamsNotificationService>.Instance,
            CreateHttpClientFactory(handler).Object,
            configuration);

    private static NotificationRequest CreateRequest(string? recipient = "https://example.com/webhook") =>
        NotificationRequest.Create("Teams")
            .WithSubject("Pipeline failed")
            .WithMessage("Pipeline XYZ failed at 03:00")
            .To(recipient is null ? Array.Empty<string>() : new[] { recipient })
            .Build();

    // ──────────────────────────────────────────── Validate ──────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidateReturnsNoWebhookUrlWhenNoRecipientsAndNoDefaultWebhookConfigured()
    {
        // Arrange
        var sut = CreateService(CreateConfiguration(defaultWebhookUrl: null), new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)));
        var request = CreateRequest(recipient: null);

        // Act
        var result = sut.Validate(request);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code!.Name.ShouldBe("NoWebhookUrl");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidateReturnsSuccessWhenNoRecipientsButDefaultWebhookUrlIsConfigured()
    {
        // Arrange
        var sut = CreateService(
            CreateConfiguration(defaultWebhookUrl: "https://default.example.com/hook"),
            new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)));
        var request = CreateRequest(recipient: null);

        // Act
        var result = sut.Validate(request);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidateReturnsEmptyMessageWhenMessageIsWhitespaceOnly()
    {
        // Arrange
        var sut = CreateService(CreateConfiguration(), new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)));
        var request = NotificationRequest.Create("Teams")
            .WithSubject("subject")
            .WithMessage("   ")
            .To("https://example.com/webhook")
            .Build();

        // Act
        var result = sut.Validate(request);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Name.ShouldBe("EmptyMessage");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/hook")]
    [InlineData("mailto:someone@example.com")]
    public void ValidateReturnsInvalidWebhookUrlWhenARecipientIsNotAnHttpOrHttpsAbsoluteUri(string badRecipient)
    {
        // Arrange
        var sut = CreateService(CreateConfiguration(), new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)));
        var request = CreateRequest(badRecipient);

        // Act
        var result = sut.Validate(request);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Name.ShouldBe("InvalidWebhookUrl");
        result.Details!.GetValue<string>("WebhookUrl").ShouldBe(badRecipient);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValidateReturnsSuccessWhenAllRecipientsAreValidHttpOrHttpsUrls()
    {
        // Arrange
        var sut = CreateService(CreateConfiguration(), new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)));
        var request = NotificationRequest.Create("Teams")
            .WithSubject("s")
            .WithMessage("m")
            .To(new[] { "http://a.example.com/hook", "https://b.example.com/hook" })
            .Build();

        // Act
        var result = sut.Validate(request);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    // ────────────────────────────────────────────── Send ────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task SendPostsToTheDefaultWebhookUrlWhenTheRequestHasNoRecipients()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateService(CreateConfiguration(defaultWebhookUrl: "https://default.example.com/hook"), handler);
        var request = CreateRequest(recipient: null);

        // Act
        var result = await sut.Send(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.IsSuccess.ShouldBeTrue();
        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri.ShouldBe(new Uri("https://default.example.com/hook"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task SendPostsAMessageCardPayloadWhenUseAdaptiveCardsIsFalse()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateService(CreateConfiguration(useAdaptiveCards: false), handler);
        var request = CreateRequest();

        // Act
        var result = await sut.Send(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var body = await handler.LastRequest!.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("MessageCard");
        body.ShouldContain("Pipeline failed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task SendPostsAnAdaptiveCardPayloadWhenUseAdaptiveCardsIsTrue()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateService(CreateConfiguration(useAdaptiveCards: true), handler);
        var request = CreateRequest();

        // Act
        var result = await sut.Send(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var body = await handler.LastRequest!.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("AdaptiveCard");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task SendReturnsAnOuterSuccessWrappingAFailedNotificationResultWhenTheWebhookReturnsANonSuccessStatus()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("boom"),
        });
        var sut = CreateService(CreateConfiguration(), handler);
        var request = CreateRequest();

        // Act
        var result = await sut.Send(request, TestContext.Current.CancellationToken);

        // Assert — the outer GenericResult is still a success; the failure rides in the notification payload.
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.IsSuccess.ShouldBeFalse();
        result.Value.ErrorMessage.ShouldNotBeNull();
        result.Value.ErrorMessage.ShouldContain("InternalServerError");
        result.Value.ErrorMessage.ShouldContain("boom");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task SendReturnsSuccessNotificationResultWhenAllRecipientWebhooksSucceed()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateService(CreateConfiguration(), handler);
        var request = NotificationRequest.Create("Teams")
            .WithSubject("s")
            .WithMessage("m")
            .To(new[] { "https://a.example.com/hook", "https://b.example.com/hook" })
            .Build();

        // Act
        var result = await sut.Send(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.IsSuccess.ShouldBeTrue();
        handler.CallCount.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task SendReturnsAFailureResultWithNoResultCodeWhenTheHttpCallThrows()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("network down"));
        var sut = CreateService(CreateConfiguration(), handler);
        var request = CreateRequest();

        // Act
        var result = await sut.Send(request, TestContext.Current.CancellationToken);

        // Assert — this failure carries an IGenericMessage (from MessageLogging), not an IResultCode.
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldBeNull();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("network down");
    }

    // ───────────────────────────────── IGenericService.Execute (generic) ────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteGenericReturnsUseSendMethodWhenCommandIsANotificationRequest()
    {
        // Arrange
        IGenericService sut = CreateService(CreateConfiguration(), new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)));
        var request = CreateRequest();

        // Act
        var result = await sut.Execute<INotificationResult>(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Name.ShouldBe("UseSendMethod");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteGenericReturnsUnsupportedCommandWithTheCommandTypeNameForAnyOtherCommand()
    {
        // Arrange
        IGenericService sut = CreateService(CreateConfiguration(), new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)));
        var command = Mock.Of<IGenericCommand>();

        // Act
        var result = await sut.Execute<int>(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Name.ShouldBe("UnsupportedCommand");
        result.Details!.GetValue<string>("CommandType").ShouldBe(command.GetType().Name);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteGenericReturnsUnsupportedCommandWithNullCommandTypeWhenCommandIsNull()
    {
        // Arrange
        IGenericService sut = CreateService(CreateConfiguration(), new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)));

        // Act
        var result = await sut.Execute<int>(null!, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Name.ShouldBe("UnsupportedCommand");
        result.Details!.GetValue<string>("CommandType").ShouldBe("null");
    }

    // ─────────────────────────────── IGenericService.Execute (non-generic) ──────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteNonGenericReturnsUseSendMethodWhenCommandIsANotificationRequest()
    {
        // Arrange
        IGenericService sut = CreateService(CreateConfiguration(), new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)));
        var request = CreateRequest();

        // Act
        var result = await sut.Execute(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Name.ShouldBe("UseSendMethod");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteNonGenericReturnsUnsupportedCommandForAnyOtherCommand()
    {
        // Arrange
        IGenericService sut = CreateService(CreateConfiguration(), new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)));
        var command = Mock.Of<IGenericCommand>();

        // Act
        var result = await sut.Execute(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Name.ShouldBe("UnsupportedCommand");
    }

    // ─────────────────────────────────────── Trivial members ────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ChannelReturnsTheTeamsChannel()
    {
        // Arrange
        var sut = CreateService(CreateConfiguration(), new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)));

        // Act & Assert
        sut.Channel.Name.ShouldBe("Teams");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ServiceTypeReturnsTeams()
    {
        // Arrange
        var sut = CreateService(CreateConfiguration(), new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)));

        // Act & Assert
        sut.ServiceType.ShouldBe("Teams");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void IsAvailableIsAlwaysTrue()
    {
        // Arrange
        var sut = CreateService(CreateConfiguration(), new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)));

        // Act & Assert
        sut.IsAvailable.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "CoreFramework")]
    public void IdReflectsTheConfigurationId()
    {
        // Arrange
        var configuration = CreateConfiguration();
        var sut = CreateService(configuration, new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)));

        // Act & Assert
        sut.Id.ShouldBe(configuration.Id.ToString());
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "CoreFramework")]
    public void DisposeDoesNotThrow()
    {
        // Arrange
        var sut = CreateService(CreateConfiguration(), new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)));

        // Act & Assert
        Should.NotThrow(sut.Dispose);
    }
}
