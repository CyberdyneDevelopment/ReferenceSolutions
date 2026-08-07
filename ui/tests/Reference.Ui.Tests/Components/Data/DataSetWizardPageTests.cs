using Fdw.Data.Components.DataSets;
using Reference.Ui.Tests.Infrastructure;

namespace Reference.Ui.Tests.Components.Data;

/// <summary>
/// App-level host tests for the DataSet wizard page as the reference-ui app routes to it.
/// These assert ONLY that the app hosts the FDW page and that its landmark renders with
/// DEFAULT (unseeded) provider contexts — they do NOT assert the FDW component's internal
/// step/field/source/submit behaviour. That internal behaviour is covered in
/// Fdw.UI.Components.Blazor.Tests (DataSetWizard component tests).
/// </summary>
public sealed class DataSetWizardPageTests : BunitContext
{
    // Why: the wizard captures both providers via @ref (typed to the real providers), so use
    // concrete subclass stubs (IS-A the real providers); register infrastructure + seed both
    // nested providers with DEFAULT contexts. The app only owns routing/hosting here.
    private void HostDefault()
    {
        this.RegisterProviderInfrastructure();
        ComponentFactories.Add(new InheritingProviderFactory<DataSetWizardProvider, StubDataSetWizardProvider>());
        ComponentFactories.Add(new InheritingProviderFactory<DataSetProvider, StubDataSetProvider>());
    }

    [Fact]
    public void HostsDataSetWizardPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<Fdw.UI.Pages.Data.Pages.DataSetWizardPage>(p =>
        {
            p.AddCascadingValue(new DataSetWizardContextSeed { Value = new DataSetWizardContext() });
            p.AddCascadingValue(new DataSetContextSeed { Value = new DataSetContext() });
        });
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedDataSetWizardPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<Fdw.UI.Pages.Data.Pages.DataSetWizardPage>(p =>
        {
            p.AddCascadingValue(new DataSetWizardContextSeed { Value = new DataSetWizardContext() });
            p.AddCascadingValue(new DataSetContextSeed { Value = new DataSetContext() });
        });
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("New DataSet", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
