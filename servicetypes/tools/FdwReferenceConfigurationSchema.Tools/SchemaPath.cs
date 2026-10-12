using System.Collections.Generic;

namespace FdwReferenceConfigurationSchema.Tools;

/// <summary>One database schema (conn, sec, auth, ...), emitted as a DatabasePath entry.</summary>
public sealed class SchemaPath
{
    /// <summary>Gets or sets the database schema name, which is also the path name the providers address.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a physical location for the path. Unused for a database path, which is found by <see cref="Name"/>.</summary>
    public string? Path { get; set; }

    /// <summary>Gets or sets the kind of path. Always <c>DatabasePath</c> for a schema.</summary>
    public string PathType { get; set; } = "DatabasePath";

    /// <summary>Gets or sets the schema's tables, in name order.</summary>
    public List<SchemaContainer> Containers { get; set; } = [];
}
