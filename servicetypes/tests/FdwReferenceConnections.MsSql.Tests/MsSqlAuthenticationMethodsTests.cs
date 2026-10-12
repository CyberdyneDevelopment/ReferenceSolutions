using Fdw.Services.Connections.MsSql.Authentication;
using System.Linq;

namespace FdwReferenceConnections.MsSql.Tests;

/// <summary>
/// FDW-1090: the MsSql connection type lists every authentication method a connection may name, each with the
/// credentials it asks for and their declared labels, so a create form can ask for them before the connection exists.
/// The connection's own credential slots carry the same declarations.
/// </summary>
public class MsSqlAuthenticationMethodsTests
{
    [Fact]
    public void EveryRegisteredMethodIsListed()
    {
        var names = new MsSqlConnectionType().AuthenticationMethods.Select(method => method.Name).ToList();

        names.ShouldBe(MsSqlAuthenticationTypes.All().Select(method => method.Name).ToList(), ignoreOrder: true);
    }

    [Fact]
    public void SqlAuthAsksForAPasswordAndDoesNotActAsTheHost()
    {
        var sqlAuth = new MsSqlConnectionType().AuthenticationMethods.Single(method => method.Name == "SqlAuth");

        sqlAuth.UsesHostIdentity.ShouldBeFalse();
        var credential = sqlAuth.Credentials.ShouldHaveSingleItem();
        credential.Field.ShouldBe("SecretKeyName");
        credential.Label.ShouldBe("Password");
        credential.Secret.ShouldBeTrue();
    }

    [Fact]
    public void WindowsAuthActsAsTheHostAndAsksForNothing()
    {
        var windows = new MsSqlConnectionType().AuthenticationMethods.Single(method => method.Name == "WindowsAuth");

        windows.UsesHostIdentity.ShouldBeTrue();
        windows.Credentials.ShouldBeEmpty();
    }

    [Fact]
    public void AConnectionsSlotsCarryTheDeclaredLabels()
    {
        var configuration = new MsSqlConnectionConfiguration { Name = "Warehouse", AuthenticationType = "SqlAuth" };

        var slots = new MsSqlConnectionType().CredentialSlots(configuration);

        slots.IsSuccess.ShouldBeTrue();
        slots.Value!.Credentials.ShouldHaveSingleItem().Label.ShouldBe("Password");
    }
}
