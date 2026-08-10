using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Authentication.Clients.Models;
using Fdw.Services.Authentication.Components.Profile;
using Reference.Ui.Components.Pages;
using Reference.Ui.Tests.Infrastructure;

namespace Reference.Ui.Tests.Components;

public sealed class ProfilePageTests : BunitContext
{
    private static readonly string[] s_adminUser = ["Admin", "User"];

    private void Swap(ProfileContext? seed = null) =>
        ComponentFactories.Add(new ProviderFactory<ProfileProvider, ProfileContext>(seed));

    private static ProfileContext WithProfile(params string[] roles)
    {
        return new ProfileContext
        {
            Profile = new GetMePayload
            {
                UserId = "u-1",
                Username = "alice",
                Email = "alice@example.com",
                Roles = roles.Length > 0 ? roles.ToList() : new List<string> { "User" }
            }
        };
    }

    [Fact]
    public void RendersLoadingSpinnerWhenNoProfileAndLoading()
    {
        Swap(new ProfileContext { IsLoading = true });
        var cut = Render<Profile>();
        // Why: the loading indicator is a `.spin` element inside `.loadwrap`, not Tailwind's
        // `.animate-spin`, after the reskin.
        cut.Find(".loadwrap .spin").ShouldNotBeNull();
    }

    [Fact]
    public void RendersErrorAndSuccessBanners()
    {
        Swap(new ProfileContext { LastResult = GenericResult.Failure(new GenericMessage { Message = "bad" }), SuccessMessage = "good" });
        var cut = Render<Profile>();
        cut.Markup.ShouldContain("bad");
        cut.Markup.ShouldContain("good");
    }

    [Fact]
    public void RendersUserInformationFromContext()
    {
        Swap(WithProfile(s_adminUser));
        var cut = Render<Profile>();
        cut.Markup.ShouldContain("alice");
        cut.Markup.ShouldContain("alice@example.com");
        cut.Markup.ShouldContain("u-1");
        cut.Markup.ShouldContain("Admin");
    }

    [Fact]
    public async Task SubmitPasswordChangeValidatesCurrentRequired()
    {
        var calls = new List<ChangePasswordRequest>();
        var ctx = new ProfileContext
        {
            Profile = WithProfile().Profile,
            OnChangePassword = r => { calls.Add(r); return Task.CompletedTask; }
        };
        Swap(ctx);
        var cut = Render<Profile>();

        ClickUpdatePassword(cut);
        await Task.Yield();

        cut.Markup.ShouldContain("Current password is required.");
        calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task SubmitPasswordChangeValidatesNewLength()
    {
        var calls = new List<ChangePasswordRequest>();
        Swap(new ProfileContext
        {
            Profile = WithProfile().Profile,
            OnChangePassword = r => { calls.Add(r); return Task.CompletedTask; }
        });
        var cut = Render<Profile>();

        cut.FindAll("input[type=password]")[0].Change("oldpw");
        cut.FindAll("input[type=password]")[1].Change("short");
        cut.FindAll("input[type=password]")[2].Change("short");
        ClickUpdatePassword(cut);
        await Task.Yield();

        cut.Markup.ShouldContain("at least 8 characters");
        calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task SubmitPasswordChangeValidatesMismatch()
    {
        var calls = new List<ChangePasswordRequest>();
        Swap(new ProfileContext
        {
            Profile = WithProfile().Profile,
            OnChangePassword = r => { calls.Add(r); return Task.CompletedTask; }
        });
        var cut = Render<Profile>();

        cut.FindAll("input[type=password]")[0].Change("oldpw");
        cut.FindAll("input[type=password]")[1].Change("newPassword1");
        cut.FindAll("input[type=password]")[2].Change("newPassword2");
        ClickUpdatePassword(cut);
        await Task.Yield();

        cut.Markup.ShouldContain("do not match");
        calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task SubmitPasswordChangeSuccessInvokesCallbackAndClears()
    {
        var calls = new List<ChangePasswordRequest>();
        Swap(new ProfileContext
        {
            Profile = WithProfile().Profile,
            OnChangePassword = r => { calls.Add(r); return Task.CompletedTask; }
        });
        var cut = Render<Profile>();

        cut.FindAll("input[type=password]")[0].Change("oldpw");
        cut.FindAll("input[type=password]")[1].Change("brandNewPw1");
        cut.FindAll("input[type=password]")[2].Change("brandNewPw1");
        ClickUpdatePassword(cut);
        await Task.Yield();

        calls.ShouldHaveSingleItem();
        calls[0].CurrentPassword.ShouldBe("oldpw");
        calls[0].NewPassword.ShouldBe("brandNewPw1");

        var after = cut.FindAll("input[type=password]");
        after[0].GetAttribute("value").ShouldBeNullOrEmpty();
        after[1].GetAttribute("value").ShouldBeNullOrEmpty();
        after[2].GetAttribute("value").ShouldBeNullOrEmpty();
    }

    [Fact]
    public async Task SubmitPreferenceNoOpWhenKeyBlank()
    {
        var calls = new List<(string K, string V)>();
        Swap(new ProfileContext
        {
            Profile = WithProfile().Profile,
            OnSetPreference = (k, v) => { calls.Add((k, v)); return Task.CompletedTask; }
        });
        var cut = Render<Profile>();
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Set").Click();
        await Task.Yield();
        calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task SubmitPreferenceTrimsAndInvokes()
    {
        var calls = new List<(string K, string V)>();
        Swap(new ProfileContext
        {
            Profile = WithProfile().Profile,
            OnSetPreference = (k, v) => { calls.Add((k, v)); return Task.CompletedTask; }
        });
        var cut = Render<Profile>();

        cut.Find("input[placeholder=Key]").Change("  theme  ");
        cut.Find("input[placeholder=Value]").Change("  dark  ");
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Set").Click();
        await Task.Yield();

        calls.ShouldHaveSingleItem();
        calls[0].K.ShouldBe("theme");
        calls[0].V.ShouldBe("dark");
    }

    // Why: the submit button label is "Update password" (it switches to "Updating..." while
    // IsLoading); match either form so the click is robust to render timing.
    private static void ClickUpdatePassword(IRenderedComponent<Profile> cut) =>
        cut.FindAll("button")
            .First(b => b.TextContent.Contains("Update password", StringComparison.Ordinal)
                     || b.TextContent.Contains("Updating", StringComparison.Ordinal))
            .Click();
}
