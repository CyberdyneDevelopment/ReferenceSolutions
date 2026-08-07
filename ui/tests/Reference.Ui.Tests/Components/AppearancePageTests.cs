using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Reference.Management.UI.Tailwind.Components.Pages;
using Reference.Ui.Tests.Infrastructure;

namespace Reference.Ui.Tests.Components;

public sealed class AppearancePageTests : BunitContext
{
    private MockHttpHandler RegisterHttp(MockHttpHandler? handler = null)
    {
        handler ??= new MockHttpHandler().WithDefault("[]");
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        Services.AddSingleton(factory.Object);
        Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        return handler;
    }

    [Fact]
    public void RendersPageChromeAndNewThemeButton()
    {
        RegisterHttp();
        var cut = Render<Appearance>();
        cut.Markup.ShouldContain("Themes");
        // Why: the action button label is "New theme" (lower-case t) after the reskin.
        cut.FindAll("button").Any(b => b.TextContent.Contains("New theme", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public void NewThemeButtonOpensForm()
    {
        RegisterHttp();
        var cut = Render<Appearance>();
        cut.FindAll("button").First(b => b.TextContent.Contains("New theme", StringComparison.Ordinal)).Click();
        // Form has a Name input + color inputs
        cut.Markup.ShouldContain("Name");
        cut.FindAll("input[type=color]").Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void NoThemesShowsEmptyMessage()
    {
        RegisterHttp();
        var cut = Render<Appearance>();
        cut.Markup.ShouldContain("No themes configured");
    }

    [Fact]
    public void RendersBackButtonToSettings()
    {
        RegisterHttp();
        var cut = Render<Appearance>();
        // The back-arrow button's @onclick navigates to /settings; verify it exists.
        cut.FindAll("button").Any(b => b.InnerHtml.Contains("M15 19l-7-7 7-7", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
