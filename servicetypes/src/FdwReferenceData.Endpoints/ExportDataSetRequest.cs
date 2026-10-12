namespace FdwReferenceDataSets.Endpoints;

/// <summary>
/// Request to stream every row of a DataSet.
/// </summary>
public sealed class ExportDataSetRequest
{
    /// <summary>Gets or sets the name of the DataSet to stream.</summary>
    public string Name { get; set; } = string.Empty;
}
