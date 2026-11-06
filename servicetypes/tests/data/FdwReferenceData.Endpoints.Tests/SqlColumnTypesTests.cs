using System.Data;
using FdwReferenceDdl.Endpoints.ColumnTypes;

namespace FdwReferenceData.Endpoints.Tests;

/// <summary>
/// A DDL column's SQL type is one of the SqlColumnTypes options (FDW-4153): named by its SqlDbType name or a listed spelling, in any case,
/// and anything else is NotFound, never NVARCHAR.
/// </summary>
public sealed class SqlColumnTypesTests
{
    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("NVarChar", SqlDbType.NVarChar)]
    [InlineData("nvarchar", SqlDbType.NVarChar)]
    [InlineData("STRING", SqlDbType.NVarChar)]
    [InlineData("int", SqlDbType.Int)]
    [InlineData("INTEGER", SqlDbType.Int)]
    [InlineData("long", SqlDbType.BigInt)]
    [InlineData("bool", SqlDbType.Bit)]
    [InlineData("BOOLEAN", SqlDbType.Bit)]
    [InlineData("numeric", SqlDbType.Decimal)]
    [InlineData("double", SqlDbType.Float)]
    [InlineData("guid", SqlDbType.UniqueIdentifier)]
    [InlineData("TEXT", SqlDbType.Text)]
    [InlineData(" DateTime2 ", SqlDbType.DateTime2)]
    public void ANameOrSpellingIsItsColumnType(string text, SqlDbType expected)
    {
        var type = SqlColumnTypes.Named(text);

        ReferenceEquals(type, SqlColumnTypes.NotFound).ShouldBeFalse();
        type.DbType.ShouldBe(expected);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("varchar2")]
    [InlineData("jsonb")]
    [InlineData("")]
    public void ATypeThatIsNoneOfThemIsNotFoundNotNVarChar(string text)
        => ReferenceEquals(SqlColumnTypes.Named(text), SqlColumnTypes.NotFound).ShouldBeTrue();
}
