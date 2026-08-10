using System.Reflection;
using Reference.Ui.Components;

namespace Reference.Ui.Tests.Components;

public sealed class RoutesTests
{
    [Fact]
    public void FdwPageAssembliesStaticIsPopulated()
    {
        // Why: Routes.razor builds `_fdwPageAssemblies` at type init from
        // `PageTypes.All()`. If FDW's page registry returned nothing, the router
        // wouldn't see any of the FDW domain pages.
        var field = typeof(Routes).GetField("_fdwPageAssemblies",
            BindingFlags.NonPublic | BindingFlags.Static);
        field.ShouldNotBeNull();

        var asms = (Assembly[])field!.GetValue(null)!;
        asms.ShouldNotBeNull();
        asms.Length.ShouldBeGreaterThan(0);
        asms.ShouldAllBe(a => a.FullName!.StartsWith("Fdw", StringComparison.Ordinal));
    }
}
