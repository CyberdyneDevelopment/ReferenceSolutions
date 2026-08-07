using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Reference.Ui.Tests.Components;

public sealed class ErrorPageTests : BunitContext
{
    private void RegisterServices(HttpContext? httpContext, string? supportContact = null)
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns(httpContext);
        Services.AddSingleton(accessor.Object);

        var config = new Mock<IConfiguration>();
        config.SetupGet(c => c[It.IsAny<string>()]).Returns((string?)null);
        config.SetupGet(c => c["SupportContact"]).Returns(supportContact);
        Services.AddSingleton(config.Object);

        Services.AddSingleton<ILogger<Reference.Management.UI.Tailwind.Components.Pages.Error>>(
            NullLogger<Reference.Management.UI.Tailwind.Components.Pages.Error>.Instance);
    }

    private static DefaultHttpContext ContextWithException(Exception ex, string path = "/failed")
    {
        var ctx = new DefaultHttpContext { TraceIdentifier = "trace-abc" };
        ctx.Features.Set<IExceptionHandlerPathFeature>(new ExceptionHandlerPathFeature(ex, path));
        return ctx;
    }

    private sealed class ExceptionHandlerPathFeature : IExceptionHandlerPathFeature
    {
        public ExceptionHandlerPathFeature(Exception error, string path) { Error = error; Path = path; }
        public Exception Error { get; }
        public string Path { get; }
        // Why: These satisfy IExceptionHandlerPathFeature members. CA1822 is suppressed because the
        // interface contract requires instance members; making them static breaks the implementation.
#pragma warning disable CA1822
        public string? Endpoint => null;
        public string? RouteValues => null;
#pragma warning restore CA1822
    }

    [Fact]
    public void RendersFallbackMessageWhenHttpContextNull()
    {
        RegisterServices(httpContext: null);
        var cut = Render<Reference.Management.UI.Tailwind.Components.Pages.Error>();
        cut.Markup.ShouldContain("An unexpected error occurred.");
    }

    [Fact]
    public void RendersFallbackMessageWhenNoExceptionFeature()
    {
        RegisterServices(httpContext: new DefaultHttpContext { TraceIdentifier = "trace-x" });
        var cut = Render<Reference.Management.UI.Tailwind.Components.Pages.Error>();
        cut.Markup.ShouldContain("An unexpected error occurred.");
        cut.Markup.ShouldContain("trace-x");
    }

    [Fact]
    public void OperationCanceledRendersAsRequestCancelledInfo()
    {
        RegisterServices(ContextWithException(new OperationCanceledException("user aborted")));
        var cut = Render<Reference.Management.UI.Tailwind.Components.Pages.Error>();
        cut.Markup.ShouldContain("Request Cancelled");
    }

    [Fact]
    public void TaskCanceledRendersAsRequestCancelled()
    {
        RegisterServices(ContextWithException(new TaskCanceledException()));
        var cut = Render<Reference.Management.UI.Tailwind.Components.Pages.Error>();
        cut.Markup.ShouldContain("Request Cancelled");
    }

    [Fact]
    public void UnauthorizedRendersAsAccessDeniedWarning()
    {
        RegisterServices(ContextWithException(new UnauthorizedAccessException()));
        var cut = Render<Reference.Management.UI.Tailwind.Components.Pages.Error>();
        cut.Markup.ShouldContain("Access Denied");
    }

    [Fact]
    public void GenericExceptionRendersAsApplicationError()
    {
        RegisterServices(ContextWithException(new InvalidOperationException()));
        var cut = Render<Reference.Management.UI.Tailwind.Components.Pages.Error>();
        cut.Markup.ShouldContain("Application Error");
    }

    [Fact]
    public void RendersFailedPathFromExceptionFeature()
    {
        RegisterServices(ContextWithException(new InvalidOperationException(), path: "/api/widgets"));
        var cut = Render<Reference.Management.UI.Tailwind.Components.Pages.Error>();
        cut.Markup.ShouldContain("/api/widgets");
    }

    [Fact]
    public void RendersSupportContactWhenConfigured()
    {
        RegisterServices(ContextWithException(new InvalidOperationException()), supportContact: "ops@example.com");
        var cut = Render<Reference.Management.UI.Tailwind.Components.Pages.Error>();
        cut.Markup.ShouldContain("ops@example.com");
    }
}
