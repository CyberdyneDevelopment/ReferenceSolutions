using Fdw.Configuration.Components.Configuration;
using Fdw.Configuration.UI.Components;
using Fdw.Operations.Clients.Models;
using Microsoft.AspNetCore.Components.Rendering;
using ConfigurationPage = Fdw.UI.Pages.Configuration.Pages.ConfigurationPage;

namespace Reference.Ui.Tests.Components.SettingsArea;

/// <summary>
/// App-level host smoke for the Configuration page (<c>/configuration</c>), an FDW page the
/// reference-ui app routes to directly. The page consumes a <see cref="ConfigurationPageProvider"/>
/// whose context is the record <see cref="ConfigurationPageContext"/> (no parameterless ctor), so a
/// dedicated stub composes the record from a DEFAULT <see cref="ConfigurationContext"/>. The app owns
/// only routing/hosting, so this asserts the page renders without throwing and surfaces the host
/// landmark. The page's branch coverage (loading, sidebar, instances, empty-types) was relocated to
/// FDW (<c>Fdw.UI.Components.Blazor.Tests/Components/Settings/ConfigurationPageTests.cs</c>).
/// </summary>
public sealed class ConfigurationPageTests : BunitContext
{
    private void HostDefault() =>
        ComponentFactories.Add(new ConfigPageFactory(new ConfigurationContext()));

    [Fact]
    public void HostsConfigurationRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<ConfigurationPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedConfigurationRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<ConfigurationPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Configuration", StringComparison.Ordinal)).ShouldBeTrue();
    }

    /// <summary>
    /// bUnit factory that swaps the real <see cref="ConfigurationPageProvider"/> for a stub which
    /// composes the <see cref="ConfigurationPageContext"/> record from a seeded
    /// <see cref="ConfigurationContext"/> (the page's context record has no parameterless ctor).
    /// </summary>
    private sealed class ConfigPageFactory : Bunit.IComponentFactory
    {
        private readonly ConfigurationContext _inner;

        public ConfigPageFactory(ConfigurationContext inner) => _inner = inner;

        public bool CanCreate(Type componentType) => componentType == typeof(ConfigurationPageProvider);

        public IComponent Create(Type componentType) => new ConfigPageProviderStub(_inner);
    }

    private sealed class ConfigPageProviderStub : ComponentBase
    {
        private readonly ConfigurationContext _inner;

        public ConfigPageProviderStub(ConfigurationContext inner) => _inner = inner;

        [Parameter] public RenderFragment<ConfigurationPageContext>? ChildContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (ChildContent is null)
            {
                return;
            }

            builder.AddContent(0, ChildContent(new ConfigurationPageContext(
                _inner,
                [],
                (_, _) => Task.CompletedTask)));
        }
    }
}
