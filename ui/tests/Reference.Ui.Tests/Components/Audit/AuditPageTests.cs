using Fdw.Operations.Components.Audit;
using Reference.Ui.Tests.Infrastructure;
using AuditPage = Fdw.UI.Pages.Operations.Pages.AuditPage;

namespace Reference.Ui.Tests.Components.Audit;

/// <summary>
/// App-level host smoke for the Audit (Execution History) page (<c>/audit</c>), an FDW page the
/// reference-ui app routes to directly. The app owns only routing/hosting, so this renders the page
/// with a DEFAULT (unseeded) <see cref="AuditContext"/> and asserts it renders without throwing and
/// surfaces the page host landmark. The page's branch/action coverage (table, footer count, state
/// badges, duration, filters, refresh, apply-filters) was relocated to FDW
/// (<c>Fdw.UI.Components.Blazor.Tests/Components/Audit/AuditPageTests.cs</c>).
/// </summary>
public sealed class AuditPageTests : BunitContext
{
    private void HostDefault() =>
        ComponentFactories.Add(new ProviderFactory<AuditProvider, AuditContext>(new AuditContext()));

    [Fact]
    public void HostsAuditRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<AuditPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedAuditRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<AuditPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Execution History", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
