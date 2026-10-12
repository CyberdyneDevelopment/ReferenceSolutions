using Fdw.Services.Connections.Results;

namespace FdwReferenceConnections.MsSql.Tests;

/// <summary>
/// FDW-1068: an MsSql connection acts as the host when the authentication method it names signs in as the host
/// (WindowsAuth, ManagedIdentity, AzureCli, EntraId other than a service principal). That decides who may save it. A
/// connection whose method is unknown, or that names none, cannot be answered for and is refused, never read as "no".
/// </summary>
public class MsSqlHostIdentityTests
{
    private static MsSqlConnectionConfiguration Configured(string authenticationType, string? azureAdMode = null)
    {
        var configuration = new MsSqlConnectionConfiguration { MaxSerialInsert = 1000, Name = "Warehouse", AuthenticationType = authenticationType };
        if (azureAdMode is not null)
            configuration.AdditionalProperties["AzureAdMode"] = azureAdMode;
        return configuration;
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData("WindowsAuth", null, true)]
    [InlineData("ManagedIdentity", null, true)]
    [InlineData("SqlAuth", null, false)]
    [InlineData("EntraId", "ServicePrincipal", false)]
    [InlineData("EntraId", "Default", true)]
    public void TheAnswerIsTheAuthenticationMethods(string method, string? mode, bool actsAsHost)
    {
        var answer = new MsSqlConnectionType().UsesHostIdentity(Configured(method, mode));

        answer.IsSuccess.ShouldBeTrue();
        answer.Value.ShouldBe(actsAsHost);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData("")]
    [InlineData("Kerberos")]
    public void AnUnknownOrMissingMethodCannotBeAnsweredFor(string method)
    {
        var answer = new MsSqlConnectionType().UsesHostIdentity(Configured(method));

        answer.IsFailure.ShouldBeTrue();
        answer.Code.ShouldBeSameAs(ConnectionsResultCodes.ByName("HostIdentityUndetermined"));
    }
}
