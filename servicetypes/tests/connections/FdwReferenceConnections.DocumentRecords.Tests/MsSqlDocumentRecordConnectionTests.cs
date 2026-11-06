using System;
using System.Threading.Tasks;
using Fdw.Documents.Records.Abstractions.Capabilities;
using Fdw.Documents.Records.Abstractions.Commands;
using Fdw.Documents.Records.Abstractions.Links;
using Fdw.Documents.Records.Abstractions.Results;

namespace FdwReferenceConnections.DocumentRecords.Tests;

/// <summary>
/// What the SQL Server DocumentRecord connection answers without reaching SQL Server (FDW-3731): its capability, the target kinds it checks,
/// and the commands it refuses before any statement is built.
/// </summary>
[Collection(nameof(DocumentRecordsTestCollection))]
public sealed class MsSqlDocumentRecordConnectionTests
{
    private static MsSqlDocumentRecordConnection Connection() => new(DocumentRecordsTestFixture.SqlServer());

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ItDeclaresAtomicRevisionWrites()
    {
        Connection().RecordCapabilities.ShouldContain(DocumentRecordCapabilities.ByName("AtomicRevisionWrites"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ACommandThatFailsItsOwnCheckIsRefusedBeforeAnyStatement()
    {
        var result = await Connection().Execute(new GetDocumentRecordCommand(DocumentRecordsTestFixture.NewId(), Guid.Empty), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeSameAs(DocumentRecordResultCodes.ByName("DocumentRecordCommandRefused"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ItChecksDocumentAndRecordTargets()
    {
        var result = await Connection().Execute(new ListLinkTargetKindsCommand(DocumentRecordsTestFixture.NewId()), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldBe([LinkTargetKinds.Document, LinkTargetKinds.Record]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ATargetKindItDoesNotCheckIsRefusedByName()
    {
        var result = await Connection().Execute(new LinkTargetExistsCommand(DocumentRecordsTestFixture.NewId(), "Vehicle", DocumentRecordsTestFixture.NewId()), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeSameAs(DocumentRecordResultCodes.ByName("DocumentLinkTargetKindUnknown"));
    }
}
