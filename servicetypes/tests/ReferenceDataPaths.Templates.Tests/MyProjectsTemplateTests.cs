using System.Linq;
using Fdw.Data.DataPaths;
using ReferenceDataPaths.Templates;
using Shouldly;
using Xunit;

namespace ReferenceDataPaths.Templates.Tests;

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
    public void JoinsTheDataPathTemplatesCollectionByName()
    {
        // Why this is the load-bearing test: the member reaches the collection through a generated
        // module initializer. Referencing this package from a test host is the whole registration path.
        DataPathTemplates.ByName("MyProjects").ShouldBeOfType<MyProjectsTemplate>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataPaths")]
    public void IsReachableByIdAndAppearsInAll()
    {
        var byId = DataPathTemplates.ById(1);

        byId.ShouldBeOfType<MyProjectsTemplate>();
        DataPathTemplates.All().ShouldContain(t => t.Name == "MyProjects");
    }

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

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataPaths")]
    public void ReturnsTheSentinelForAnUnknownTemplate()
        => DataPathTemplates.ByName("NoSuchTemplate").ShouldBeSameAs(DataPathTemplates.NotFound);
}
