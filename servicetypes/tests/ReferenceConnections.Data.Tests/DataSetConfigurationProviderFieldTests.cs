using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// Unit tests for <see cref="DataSetConfigurationProvider.GetFields"/> and
/// <see cref="DataSetConfigurationProvider.SaveFields"/>.
///
/// Only <see cref="IConfigurationGateway"/> is faked. The real provider code runs under test,
/// including the DataFieldConfiguration → DataSetFieldDefinition column name projection
/// (Name→FieldName, TypeName→ScalarTypeName, IsRequired→!IsNullable).
/// </summary>
[Trait("Priority", "P1")]
public class DataSetConfigurationProviderFieldTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static DataSetConfigurationProvider MakeProvider(Mock<IConfigurationGateway> gateway)
    {

        return new DataSetConfigurationProvider(
            NullLogger<DataSetConfigurationProvider>.Instance,
            new Lazy<IConfigurationGateway>(() => gateway.Object));
    }

    private static DataFieldConfiguration Field(
        Guid dataSetId, string name, string typeName,
        bool isRequired = false, int ordinal = 0, string? description = null)
        => new DataFieldConfiguration
        {
            Id = Guid.CreateVersion7(),
            DataSetId = dataSetId,
            Name = name,
            TypeName = typeName,
            IsRequired = isRequired,
            Ordinal = ordinal,
            Description = description,
            IsCurrent = true,
            IsDeleted = false
        };

    // ── GetFields ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetFields_WhenGatewayReturnsRows_ProjectsToDefinitions()
    {
        var dataSetId = Guid.NewGuid();
        var gateway = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gateway.Setup(g => g.Execute<IEnumerable<DataFieldConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<DataFieldConfiguration>>.Success([
                Field(dataSetId, "CustomerName", "String", isRequired: true, ordinal: 0),
                Field(dataSetId, "Revenue", "Decimal", isRequired: false, ordinal: 1, description: "Annual revenue")
            ]));

        var provider = MakeProvider(gateway);

        var result = await provider.GetFields(dataSetId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(2);

        var first = result.Value[0];
        first.FieldName.ShouldBe("CustomerName");
        first.ScalarTypeName.ShouldBe("String");
        first.IsNullable.ShouldBeFalse(); // IsRequired=true → IsNullable=false
        first.Ordinal.ShouldBe(0);

        var second = result.Value[1];
        second.FieldName.ShouldBe("Revenue");
        second.ScalarTypeName.ShouldBe("Decimal");
        second.IsNullable.ShouldBeTrue(); // IsRequired=false → IsNullable=true
        second.Description.ShouldBe("Annual revenue");
    }

    [Fact]
    public async Task GetFields_WhenGatewayReturnsEmpty_ReturnsEmptyList()
    {
        var dataSetId = Guid.NewGuid();
        var gateway = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gateway.Setup(g => g.Execute<IEnumerable<DataFieldConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<DataFieldConfiguration>>.Success([]));

        var provider = MakeProvider(gateway);

        var result = await provider.GetFields(dataSetId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetFields_WhenGatewayFails_ReturnsFailure()
    {
        var dataSetId = Guid.NewGuid();
        var gateway = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gateway.Setup(g => g.Execute<IEnumerable<DataFieldConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<DataFieldConfiguration>>.Failure(new GenericMessage("DB offline")));

        var provider = MakeProvider(gateway);

        var result = await provider.GetFields(dataSetId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // ── SaveFields ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveFields_WithNonEmptyList_RetiresExistingAndInsertsNew()
    {
        var dataSetId = Guid.NewGuid();
        var gateway = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        // Both retire (Update<int>) and insert (Insert<int>) succeed
        gateway.Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Success(1));

        var provider = MakeProvider(gateway);

        var fields = new List<DataSetFieldDefinition>
        {
            new() { DataSetId = dataSetId, FieldName = "Id", ScalarTypeName = "Guid", IsNullable = false, Ordinal = 0 },
            new() { DataSetId = dataSetId, FieldName = "Name", ScalarTypeName = "String", IsNullable = true, Ordinal = 1 }
        };

        var result = await provider.SaveFields(dataSetId, fields, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        // Why: SaveFields does 1 retire + 1 ConfigurationSaveCommand insert PER FIELD (the per-field
        // child-save pattern), so 2 fields = 1 retire + 2 inserts = 3 Execute<int> calls.
        gateway.Verify(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task SaveFields_WithEmptyList_RetiresOnlyWithoutInsert()
    {
        var dataSetId = Guid.NewGuid();
        var gateway = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gateway.Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Success(0));

        var provider = MakeProvider(gateway);

        var result = await provider.SaveFields(dataSetId, [], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        // Only retire call; no insert
        gateway.Verify(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveFields_WhenRetireFails_ReturnsFailureWithoutInsert()
    {
        var dataSetId = Guid.NewGuid();
        var gateway = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gateway.Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Failure(new GenericMessage("Retire failed")));

        var provider = MakeProvider(gateway);

        var fields = new List<DataSetFieldDefinition>
        {
            new() { DataSetId = dataSetId, FieldName = "Id", ScalarTypeName = "Guid" }
        };

        var result = await provider.SaveFields(dataSetId, fields, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        // Only one call (the retire) — should NOT proceed to insert on failure
        gateway.Verify(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
