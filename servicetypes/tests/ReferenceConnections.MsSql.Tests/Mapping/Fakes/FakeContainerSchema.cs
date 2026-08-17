using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Schema;
using Fdw.Schema.Indexes;
using Fdw.Schema.Keys;
using Fdw.Schema.Properties;
using Fdw.Schema.Schemas;

namespace ReferenceConnections.MsSql.Tests.Mapping.Fakes;

/// <summary>
/// Test double for IContainerSchema for unit testing.
/// </summary>
internal sealed class FakeContainerSchema : IContainerSchema
{
    public IReadOnlyList<IField> Fields { get; init; } = [];

    public bool SupportsNesting { get; init; }

    // ========================================
    // ISchemaDefinition<IField> Members
    // ========================================

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public IReadOnlyList<IField> Properties => Fields;

    public IKeyDefinition<IField>? SurrogateKey { get; init; }

    public IKeyDefinition<IField>? NaturalKey { get; init; }

    public IReadOnlyList<IIndexDefinition<IField>> Indexes { get; init; } = [];

    public IDataLayout Layout { get; init; } = DataLayouts.ByName("Tabular");

    public IReadOnlyList<ISchemaDefinition<IField>>? Children { get; init; }

    public string? PathExpression { get; init; }


    public IReadOnlyList<IField> Get(IPropertyRole role)
    {
        if (role.IsKeyRole)
            return GetIdentityFields();
        if (role.IsAggregatable)
            return GetMeasureFields();
        if (!role.IsKeyRole && !role.IsAggregatable)
            return GetAttributeFields();

        return [];
    }

    // ========================================
    // IContainerSchema Members
    // ========================================

    public IReadOnlyList<IField> GetIdentityFields() =>
        Fields.Where(f => f.Role.IsKeyRole).ToList();

    // Mirrors ContainerSchema.GetProjectableFields exactly — a fake that returned every field
    // regardless of visibility would let a translator that ignores Visibility pass here and emit a
    // NotVisible storage column in a real SELECT.
    public IReadOnlyList<IField> GetProjectableFields() =>
        Fields.Where(f => f.Visibility.AllowsProjection).ToList();

    public IReadOnlyList<IField> GetAttributeFields() =>
        Fields.Where(f => !f.Role.IsKeyRole && !f.Role.IsAggregatable).ToList();

    public IReadOnlyList<IField> GetMeasureFields() =>
        Fields.Where(f => f.Role.IsAggregatable).ToList();

    public IField? Get(string name) =>
        Fields.FirstOrDefault(f => string.Equals(f.Name, name, System.StringComparison.OrdinalIgnoreCase));

    public static FakeContainerSchema Create(params IField[] fields)
    {
        return new FakeContainerSchema { Fields = fields };
    }
}
