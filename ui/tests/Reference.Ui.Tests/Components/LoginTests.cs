namespace Reference.Ui.Tests.Components;

public sealed class LoginTests : BunitContext
{
    private void Navigate(string url)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(url);
    }

    [Theory]
    [InlineData("invalid-credentials", "ACCESS DENIED: Invalid credentials")]
    [InlineData("server-error", "ACCESS DENIED: Server error occurred")]
    public void RendersKnownErrorMessagesFromQueryString(string error, string expected)
    {
        Navigate($"/login?error={error}");
        var cut = Render<Login>();
        cut.Markup.ShouldContain(expected);
    }

    [Fact]
    public void UnknownErrorCodeShowsNoErrorMessage()
    {
        Navigate("/login?error=mystery");
        var cut = Render<Login>();
        cut.Markup.ShouldNotContain("ACCESS DENIED:");
    }

    [Fact]
    public void NoErrorParamShowsNoErrorMessage()
    {
        Navigate("/login");
        var cut = Render<Login>();
        cut.Markup.ShouldNotContain("ACCESS DENIED:");
    }

    [Fact]
    public void ReturnUrlFromQueryFlowsToHiddenInput()
    {
        Navigate("/login?returnUrl=/dashboard/connectors");
        var cut = Render<Login>();
        var hidden = cut.Find("input[name=returnUrl]");
        hidden.GetAttribute("value").ShouldBe("/dashboard/connectors");
    }

    [Fact]
    public void ReturnUrlDefaultIsRootWhenMissing()
    {
        Navigate("/login");
        var cut = Render<Login>();
        cut.Find("input[name=returnUrl]").GetAttribute("value").ShouldBe("/");
    }

    [Fact]
    public void RendersLoginFormPostsToAuthLogin()
    {
        Navigate("/login");
        var cut = Render<Login>();
        var form = cut.Find("form");
        form.GetAttribute("method").ShouldBe("post");
        form.GetAttribute("action").ShouldBe("/auth/login");
        cut.Find("input[name=username]").ShouldNotBeNull();
        cut.Find("input[name=password]").ShouldNotBeNull();
        cut.Find("input[name=tenant]").ShouldNotBeNull();
    }
}
