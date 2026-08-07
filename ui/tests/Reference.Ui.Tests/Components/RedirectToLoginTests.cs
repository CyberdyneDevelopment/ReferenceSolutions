namespace Reference.Ui.Tests.Components;

public sealed class RedirectToLoginTests : BunitContext
{
    [Fact]
    public void NavigatesToLoginWithEncodedReturnUrl()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/some/path?x=1");

        Render<RedirectToLogin>();

        nav.Uri.ShouldContain("/login?returnUrl=");
        nav.Uri.ShouldContain(Uri.EscapeDataString("some/path?x=1"));
    }
}
