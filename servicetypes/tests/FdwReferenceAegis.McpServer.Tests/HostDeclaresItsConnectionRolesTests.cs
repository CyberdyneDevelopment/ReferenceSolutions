using Fdw.Services.Data;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceAegis.McpServer.Tests;

/// <summary>
/// The host starts only with the connection roles it declares in <c>aegisSchema.json</c> (FDW-1121): its one
/// FileSystem connection plays both the platform and the server role, the registration reads that declaration, and
/// the same schema with the declaration removed does not start.
/// </summary>
public sealed class HostDeclaresItsConnectionRolesTests
{
    private const string SchemaFileName = "aegisSchema.json";
    private const string Host = "FdwReferenceAegis.McpServer";

    [Fact]
    public void TheShippedSchemaDeclaresOneConnectionAsPlatformAndServer()
    {
        var roles = ConfigurationSchemaLoader.Load(SchemaFileName).ConnectionRoles;

        roles.ShouldNotBeNull();
        roles.Platform.ShouldBe("ServerConfiguration");
        roles.Server.ShouldBe("ServerConfiguration");
    }

    [Fact]
    public void TheShippedSchemaPassesTheStartupDeclaration()
    {
        var result = ConnectionRoleDeclaration.Declare(ConfigurationSchemaLoader.Load(SchemaFileName), Host, NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }

    [Fact]
    public void TheShippedSchemaWithoutItsRolesFailsNamingTheHost()
    {
        var schema = ConfigurationSchemaLoader.Load(SchemaFileName);
        schema.ConnectionRoles = null;

        var result = ConnectionRoleDeclaration.Declare(schema, Host, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain(Host);
    }
}
