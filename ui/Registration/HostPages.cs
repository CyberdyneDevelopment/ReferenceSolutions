using System.Collections.Generic;
using Fdw.UI.Registration;

namespace Reference.Management.UI.Tailwind.Registration;

/// <summary>
/// The pages this host owns, declared the same way a package declares its own.
/// </summary>
// Why: these three components live in THIS assembly, so no package can declare them — an IPage requires
// the component type. They are deliberately NOT a PageType: a PageType contributes assemblies to the
// Blazor Router's AdditionalAssemblies, and this assembly is already the Router's AppAssembly, so
// registering it there would re-add it and break route resolution. An IPage list carries no assembly
// registration, so the host publishes its pages without that side effect and the sidebar stays built
// from one shape for every page, host-owned or packaged.
public static class HostPages
{
    /// <summary>
    /// Gets the host-owned pages, each carrying its own sidebar entry.
    /// </summary>
    public static IReadOnlyList<IPage> All { get; } =
    [
        // Why: an empty section renders as an unlabelled block above the titled sections, which is where
        // the Dashboard link sat when the nav was hardcoded.
        new Page(
            "Home",
            typeof(Components.Pages.Home),
            new NavItem("Dashboard", "dashboard", null, 0),
            null),
        new Page(
            "Profile",
            typeof(Components.Pages.Profile),
            new NavItem("Profile", "user", NavSections.Security, 90),
            null),
        new Page(
            "LocalConfig",
            typeof(Components.Pages.LocalConfig),
            new NavItem("Local Config (FS)", "database", NavSections.Configuration, 95),
            null),
    ];
}
