using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace ReferenceConnections.MsSql.Tests.Mapping.Fakes;

/// <summary>
/// Test double for IStorageContainer for unit testing.
/// </summary>
internal sealed class FakeStorageContainer : IStorageContainer
{
    public string Name { get; init; } = "TestContainer";
    public IContainerType ContainerType { get; init; } = null!;
    public IFormatType Format { get; init; } = null!;
    public IContainerSchema Schema { get; init; } = new FakeContainerSchema();
    public IPath Path { get; init; } = null!;
    public string[] SupportedOperations { get; init; } = [];
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();

    public static FakeStorageContainer Create(params IField[] fields)
    {
        return new FakeStorageContainer
        {
            Schema = FakeContainerSchema.Create(fields)
        };
    }
}
