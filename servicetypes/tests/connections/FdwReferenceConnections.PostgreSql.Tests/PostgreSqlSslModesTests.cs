using Fdw.Services.Connections.PostgreSql;
using FdwReferenceConnections.PostgreSql;
using FdwReferenceConnections.PostgreSql.SslModes;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace FdwReferenceConnections.PostgreSql.Tests;

/// <summary>
/// A PostgreSQL connection's SslMode is one of the PostgreSqlSslModes options (FDW-4153): each carries the Npgsql value it is, and the
/// factory refuses a name that is none of them instead of parsing an enum.
/// </summary>
public sealed class PostgreSqlSslModesTests
{
    public static TheoryData<string, SslMode> Modes => new()
    {
        { "Disable", SslMode.Disable },
        { "Allow", SslMode.Allow },
        { "Prefer", SslMode.Prefer },
        { "Require", SslMode.Require },
        { "VerifyCA", SslMode.VerifyCA },
        { "VerifyFull", SslMode.VerifyFull },
    };

    [Theory]
    [Trait("Priority", "P1")]
    [MemberData(nameof(Modes))]
    public void EachModeCarriesItsNpgsqlValue(string name, SslMode expected)
    {
        var mode = PostgreSqlSslModes.ByName(name);

        ReferenceEquals(mode, PostgreSqlSslModes.NotFound).ShouldBeFalse();
        mode.NpgsqlMode.ShouldBe(expected);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [InlineData("prefer")]
    [InlineData("Sometimes")]
    [InlineData("")]
    public void ANameThatIsNoModeIsNotFound(string name)
        => ReferenceEquals(PostgreSqlSslModes.ByName(name), PostgreSqlSslModes.NotFound).ShouldBeTrue();

    [Fact]
    [Trait("Priority", "P0")]
    public void TheFactoryRefusesAnSslModeThatIsNoModeAndNamesIt()
    {
        var factory = new PostgreSqlConnectionFactory(
            NullLogger<PostgreSqlConnectionFactory>.Instance, NullLogger<PostgreSqlConnection>.Instance);

        var result = factory.Create(
            new PostgreSqlConnectionConfiguration
            {
                Name = "Warehouse",
                Host = "localhost",
                Database = "TestDb",
                Port = 5432,
                SslMode = "Sometimes",
                CommandTimeout = 30,
                ConnectionTimeout = 15,
                MaxSerialInsert = 1000,
                MinPoolSize = 0,
                MaxPoolSize = 10,
                DefaultSchema = "public",
                AuthenticationType = "Password",
            },
            resolvedSecret: null);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Sometimes");
    }
}
