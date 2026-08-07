using Fdw.Services.Authorization.Components.Roles;
using Reference.Ui.Tests.Infrastructure;
using RolesPage = Fdw.UI.Pages.Authorization.Pages.RolesPage;
using RoleDetailPage = Fdw.UI.Pages.Authorization.Pages.RoleDetailPage;

namespace Reference.Ui.Tests.Components.Auth;

/// <summary>
/// App-level host tests for the Roles list and RoleDetail pages as the reference-ui app routes to
/// them. These assert ONLY that the app hosts the FDW pages and that their landmarks render with a
/// DEFAULT (unseeded) provider context — they do NOT assert the FDW component's internal
/// list/create/delete/permission behaviour. That internal behaviour is covered in
/// Fdw.UI.Components.Blazor.Tests (Auth/RolesPageContentTests + Auth/RoleDetailPageContentTests).
/// </summary>
public sealed class RolesPageTests : BunitContext
{
    // Why: swap the live FDW RoleProvider for a stub seeded with a DEFAULT context so the page
    // renders deterministically without HTTP — the app only owns routing/hosting here.
    private void HostDefault() =>
        ComponentFactories.Add(new ProviderFactory<RoleProvider, RoleContext>(new RoleContext()));

    [Fact]
    public void HostsRolesPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<RolesPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedRolesPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<RolesPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Roles", StringComparison.Ordinal)).ShouldBeTrue();
    }

    // Why: RoleDetail captures its RoleProvider via @ref and casts to the concrete RoleProvider
    // type, which rejects a ProviderStub. So host it against the REAL RoleProvider wired to a no-op
    // HTTP factory (empty responses); the page header (RoleName) renders regardless of load state.
    [Fact]
    public void HostsRoleDetailPageRendersWithRoleNameLandmark()
    {
        this.RegisterProviderInfrastructure();
        var cut = Render<RoleDetailPage>(p => p.Add(d => d.RoleName, "Admin"));
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Admin", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
