using Reference.Management.UI.Tailwind.Helpers;

namespace Reference.Ui.Tests.Helpers;

public sealed class SqlTypeMapperTests
{
    [Theory]
    [InlineData("INT", "Int32")]
    [InlineData("integer", "Int32")]
    [InlineData("BIGINT", "Int64")]
    [InlineData("smallint", "Int32")]
    [InlineData("tinyint", "Int32")]
    [InlineData("bit", "Boolean")]
    [InlineData("decimal", "Decimal")]
    [InlineData("numeric", "Decimal")]
    [InlineData("money", "Decimal")]
    [InlineData("smallmoney", "Decimal")]
    [InlineData("float", "Double")]
    [InlineData("real", "Double")]
    [InlineData("datetime", "DateTime")]
    [InlineData("datetime2", "DateTime")]
    [InlineData("smalldatetime", "DateTime")]
    [InlineData("date", "DateTime")]
    [InlineData("datetimeoffset", "DateTime")]
    [InlineData("time", "String")]
    [InlineData("uniqueidentifier", "Guid")]
    [InlineData("nvarchar", "String")]
    [InlineData("varchar", "String")]
    [InlineData("char", "String")]
    [InlineData("nchar", "String")]
    [InlineData("ntext", "String")]
    [InlineData("text", "String")]
    [InlineData("varbinary", "Byte[]")]
    [InlineData("binary", "Byte[]")]
    [InlineData("image", "Byte[]")]
    [InlineData("xml", "String")]
    [InlineData("geography", "String")]
    public void MapSqlTypeToFieldType_KnownAndUnknown_Maps(string input, string expected)
    {
        SqlTypeMapper.MapSqlTypeToFieldType(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData("nvarchar(255)", "String")]
    [InlineData("DECIMAL(18, 2)", "Decimal")]
    [InlineData("  varbinary(MAX)  ", "Byte[]")]
    public void MapSqlTypeToFieldType_StripsParenthesizedSizeAndTrims(string input, string expected)
    {
        SqlTypeMapper.MapSqlTypeToFieldType(input).ShouldBe(expected);
    }
}
