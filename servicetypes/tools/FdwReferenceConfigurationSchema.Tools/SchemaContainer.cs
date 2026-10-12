using System.Collections.Generic;

namespace FdwReferenceConfigurationSchema.Tools;

/// <summary>One table, read from sys.tables within a schema.</summary>
public sealed class SchemaContainer
{
    /// <summary>Gets or sets the table name, without its schema.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the container kind. Always <c>Table</c>: the generator reads tables only, never views.</summary>
    public string ContainerType { get; set; } = "Table";

    /// <summary>Gets or sets the format the container's rows are read in. Always <c>Table</c> for a database container.</summary>
    public string Format { get; set; } = "Table";

    /// <summary>Gets or sets the table's columns, in column_id order.</summary>
    public List<SchemaField> Fields { get; set; } = [];

    /// <summary>
    /// Gets or sets the table's keys: the physical primary key, the logical key on <c>Id</c> when that
    /// column exists, and a foreign key to each structural parent.
    /// </summary>
    public List<SchemaKey> Keys { get; set; } = [];
}
