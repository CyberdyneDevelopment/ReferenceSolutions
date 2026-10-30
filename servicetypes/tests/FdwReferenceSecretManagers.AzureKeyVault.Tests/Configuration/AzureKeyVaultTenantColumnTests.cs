using FdwReferenceSecretManagers.AzureKeyVault.Configuration;

namespace FdwReferenceSecretManagers.AzureKeyVault.Tests.Configuration;

/// <summary>
/// FDW-3041: TenantId on the configuration is the Azure tenant and is stored in the AzureTenantId column. The TenantId column is the
/// platform tenant every row carries (a Guid), so a mapper that read the property from it failed on every stored row.
/// </summary>
public class AzureKeyVaultTenantColumnTests
{
    [Fact]
    [Trait("Priority", "P0")]
    public void TheAzureTenantIsReadFromTheAzureTenantIdColumnNotThePlatformTenantColumn()
    {
        IDictionary<string, object?> row = new Dictionary<string, object?>
        {
            ["TenantId"] = Guid.NewGuid(),
            ["AzureTenantId"] = "contoso-tenant"
        };

        var mapped = new AzureKeyVaultConfigurationPocoMapper().MapFromDictionary(row);

        mapped.IsSuccess.ShouldBeTrue();
        mapped.Value.ShouldBeOfType<AzureKeyVaultConfiguration>().TenantId.ShouldBe("contoso-tenant");
    }
}
