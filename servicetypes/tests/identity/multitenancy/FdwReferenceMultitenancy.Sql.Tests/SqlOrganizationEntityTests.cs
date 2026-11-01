using FdwReferenceMultitenancy.Sql.Models;
using Shouldly;

namespace FdwReferenceMultitenancy.Sql.Tests;

/// <summary>
/// Proves the SQL organization entity hands <c>DefaultWorkspaceId</c> to the organization configuration unchanged,
/// including the stated "no default workspace" (FDW-1060).
/// </summary>
[Trait("Priority", "P1")]
public class SqlOrganizationEntityTests
{
    [Fact]
    public void ToConfigurationCarriesTheDefaultWorkspaceId()
    {
        var workspace = Guid.NewGuid();
        var entity = new SqlOrganizationEntity { Id = Guid.NewGuid(), Slug = "default", DefaultWorkspaceId = workspace };

        entity.ToConfiguration().DefaultWorkspaceId.ShouldBe(workspace);
    }

    [Fact]
    public void ToConfigurationKeepsAnOrganizationWithNoDefaultWorkspaceAsNull()
    {
        var entity = new SqlOrganizationEntity { Id = Guid.NewGuid(), Slug = "default" };

        entity.ToConfiguration().DefaultWorkspaceId.ShouldBeNull();
    }
}
