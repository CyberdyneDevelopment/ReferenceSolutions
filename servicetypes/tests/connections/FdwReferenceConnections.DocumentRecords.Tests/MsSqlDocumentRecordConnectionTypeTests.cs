using System.Linq;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Abstractions.DocumentRecords;
using Fdw.Services.Connections.Abstractions.Purposes;

namespace FdwReferenceConnections.DocumentRecords.Tests;

/// <summary>
/// A SQL Server connection becomes a DocumentRecord connection through this package's extension (FDW-3731), and only a SQL Server one;
/// with it, a SQL Server connection fits the DocumentRecords purpose a grant names.
/// </summary>
[Collection(nameof(DocumentRecordsTestCollection))]
public sealed class MsSqlDocumentRecordConnectionTypeTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ThePackageRegistersTheSqlServerExtension()
    {
        DocumentRecordConnectionTypes.All().Select(type => type.Name).ShouldContain("MsSql");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASqlServerConnectionIsAnsweredAsADocumentRecordConnection()
    {
        DocumentRecordConnections.Of(DocumentRecordsTestFixture.SqlServer()).ShouldBeOfType<MsSqlDocumentRecordConnection>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheExtensionWrapsNoOtherKindOfConnection()
    {
        new MsSqlDocumentRecordConnectionType().Adapt(new Moq.Mock<IGenericConnection>().Object).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASqlServerConnectionFitsTheDocumentRecordsPurpose()
    {
        ConnectionPurposes.ByName("DocumentRecords").Unfit(DocumentRecordsTestFixture.SqlServer()).ShouldBeNull();
    }
}
