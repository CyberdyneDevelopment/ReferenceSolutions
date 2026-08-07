using Fdw.Services.Authorization.Components.Roles;
using Fdw.Services.Authorization.Components.Users;
using Reference.Ui.Tests.Infrastructure;
using UsersPage = Fdw.UI.Pages.Authorization.Pages.UsersPage;

namespace Reference.Ui.Tests.Components.Auth;

/// <summary>
/// App-level host tests for the Users page as the reference-ui app routes to it. These assert ONLY
/// that the app hosts the FDW page and that its landmark renders with a DEFAULT (unseeded) provider
/// context — they do NOT assert the FDW component's internal list/badge/form/validation/action
/// behaviour. That internal behaviour is covered in
/// Fdw.UI.Components.Blazor.Tests (Auth/UsersPageContentTests).
/// </summary>
public sealed class UsersPageTests : BunitContext
{
    // Why: swap the live FDW UserProvider (and the nested RoleProvider used by the create/edit form)
    // for stubs seeded with DEFAULT contexts so the page renders deterministically without HTTP —
    // the app only owns routing/hosting here.
    private void HostDefault()
    {
        ComponentFactories.Add(new ProviderFactory<UserProvider, UserContext>(new UserContext()));
        ComponentFactories.Add(new ProviderFactory<RoleProvider, RoleContext>(new RoleContext()));
    }

    [Fact]
    public void HostsUsersPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<UsersPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedUsersPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<UsersPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Users", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
