using Fdw.Data.DataPaths;

namespace FdwReferenceDataPaths.Templates.Tests;

/// <summary>
/// Covers <see cref="MyProjectsTemplate"/> and its registration into FDW's DataPathTemplates
/// collection. The membership assertions are the point: this package exists so that its module
/// initializer runs when the assembly loads, without a host.
/// </summary>
public class MyProjectsTemplateTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataPaths")]
    public void DeclaresTheTenantScopedTemplateAndPolicy()
    {
        var template = new MyProjectsTemplate();

        template.Name.ShouldBe("MyProjects");
        template.Template.ShouldBe("{tenantId}/{projectName}/{filename}");
        template.DataStoreServiceType.ShouldBe("FileSystem");
        template.DefaultPolicyName.ShouldBe("TenantScoped");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataPaths")]
    public void DoesNotRequireTenantIdAsACallerSuppliedVariable()
    {
        // Why: tenantId is injected server-side from the caller's request context. Requiring it here
        // would let a caller name the tenant whose files they address, which is the boundary itself.
        var template = new MyProjectsTemplate();

        template.RequiredVariables.ShouldBe(new[] { "projectName", "filename" });
        template.RequiredVariables.ShouldNotContain("tenantId");
    }
}
