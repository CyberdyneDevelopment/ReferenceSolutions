using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Schema;

namespace ReferenceConnections.MsSql.Tests.Mapping.Fakes;

/// <summary>
/// Test double for IField for unit testing.
/// </summary>
internal sealed class FakeField : IField
{
    public string Name { get; init; } = string.Empty;
    public IFieldType FieldType { get; init; } = null!;
    public IPropertyRole Role { get; init; } = PropertyRoles.ByName("Attribute");
    public bool IsRequired => !IsNullable;
    public bool IsNullable { get; init; }
    public string? Description { get; init; }
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
    public string? TypeSystemId { get; init; }
    public int? ConverterTypeId { get; init; }
    public bool IsIdentity { get; init; }
    public bool IsComputed { get; init; }
    public bool IsSystemProvided { get; init; }

    public static FakeField Create(string name, int? converterTypeId = null)
    {
        return new FakeField
        {
            Name = name,
            ConverterTypeId = converterTypeId,
            TypeSystemId = converterTypeId.HasValue ? "MsSql" : null
        };
    }
}
