using System.Diagnostics.CodeAnalysis;

namespace Reference.Api.Endpoints;

/// <summary>
/// Record for INFORMATION_SCHEMA.TABLES query result.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class TableSchemaRecord
{
    public string TABLE_SCHEMA { get; set; } = string.Empty;
    public string TABLE_NAME { get; set; } = string.Empty;
    public string TABLE_TYPE { get; set; } = string.Empty;
}
