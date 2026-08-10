namespace Reference.Ui.Helpers;

/// <summary>
/// Maps SQL column types to .NET field types and infers field roles.
/// </summary>
public static class SqlTypeMapper
{
    /// <summary>
    /// Maps a SQL data type string to the corresponding .NET field type name.
    /// </summary>
    public static string MapSqlTypeToFieldType(string sqlType)
    {
        var normalized = sqlType.ToUpperInvariant().Split('(')[0].Trim();
        return normalized switch
        {
            "INT" or "INTEGER" => "Int32",
            "BIGINT" => "Int64",
            "SMALLINT" or "TINYINT" => "Int32",
            "BIT" => "Boolean",
            "DECIMAL" or "NUMERIC" or "MONEY" or "SMALLMONEY" => "Decimal",
            "FLOAT" => "Double",
            "REAL" => "Double",
            "DATETIME" or "DATETIME2" or "SMALLDATETIME" or "DATE" => "DateTime",
            "DATETIMEOFFSET" => "DateTime",
            "TIME" => "String",
            "UNIQUEIDENTIFIER" => "Guid",
            "NVARCHAR" or "VARCHAR" or "CHAR" or "NCHAR" or "NTEXT" or "TEXT" => "String",
            "VARBINARY" or "BINARY" or "IMAGE" => "Byte[]",
            "XML" => "String",
            _ => "String"
        };
    }


}
