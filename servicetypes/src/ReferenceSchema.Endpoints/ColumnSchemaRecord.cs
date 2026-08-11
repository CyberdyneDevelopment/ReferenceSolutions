using System.Diagnostics.CodeAnalysis;

namespace ReferenceSchema.Endpoints;

/// <summary>
/// Record for INFORMATION_SCHEMA.COLUMNS query result.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class ColumnSchemaRecord
{
    public string TABLE_SCHEMA { get; set; } = string.Empty;
    public string TABLE_NAME { get; set; } = string.Empty;
    public string COLUMN_NAME { get; set; } = string.Empty;
    public int ORDINAL_POSITION { get; set; }
    public string? COLUMN_DEFAULT { get; set; }
    public string IS_NULLABLE { get; set; } = "YES";
    public string DATA_TYPE { get; set; } = string.Empty;
    public int? CHARACTER_MAXIMUM_LENGTH { get; set; }
    public int? NUMERIC_PRECISION { get; set; }
    public int? NUMERIC_SCALE { get; set; }
}
