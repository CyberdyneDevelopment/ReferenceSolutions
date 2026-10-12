using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FdwReferenceConfigurationSchema.Tools;

/// <summary>A physical, logical, or foreign key on a <see cref="SchemaContainer"/>.</summary>
public sealed class SchemaKey
{
    /// <summary>Gets or sets the key name: <c>PK_{table}</c>, <c>LK_{table}_Id</c>, or <c>FK_{table}_{parent}</c>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind of key: <c>Physical</c>, <c>Logical</c>, or <c>Foreign</c>.</summary>
    public string TypeId { get; set; } = string.Empty;

    /// <summary>Gets or sets the parent table a foreign key points at; <see langword="null"/>, and omitted from the JSON, for any other kind.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReferencedContainerName { get; set; }

    /// <summary>Gets or sets the parent's primary key a foreign key points at (<c>PK_{parent}</c>); <see langword="null"/>, and omitted from the JSON, for any other kind.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReferencedKeyName { get; set; }

    /// <summary>Gets or sets the columns the key is made of, in key order.</summary>
    public List<SchemaKeyField> KeyFields { get; set; } = [];
}
