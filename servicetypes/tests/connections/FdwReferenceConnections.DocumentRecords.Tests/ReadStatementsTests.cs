using System;
using System.Data;
using Fdw.Documents.Records.Abstractions.Links;
using FdwReferenceConnections.DocumentRecords.Sql;

namespace FdwReferenceConnections.DocumentRecords.Tests;

/// <summary>
/// What SQL Server is sent for a read of an organization's records (FDW-3731): every read filters by the organization, a list of what
/// belongs to one entity first finds that entity, and the definitions come from the platform, the tenant and the organization.
/// </summary>
[Collection(nameof(DocumentRecordsTestCollection))]
public sealed class ReadStatementsTests
{
    private static readonly Guid Organization = DocumentRecordsTestFixture.NewId();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AListOfADocumentsFilesFindsTheDocumentFirst()
    {
        var document = DocumentRecordsTestFixture.NewId();

        var statement = ReadStatements.Media(Organization, document);

        statement.MustFind.ShouldBe("Document");
        statement.MustFindId.ShouldBe(document);
        statement.Text.ShouldStartWith("SELECT CASE WHEN EXISTS");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void EveryReadFiltersByTheOrganization()
    {
        var id = DocumentRecordsTestFixture.NewId();
        RecordStatement[] statements =
        [
            ReadStatements.Document(Organization, id), ReadStatements.Documents(Organization, "Permit"), ReadStatements.Media(Organization, id),
            ReadStatements.Node(Organization, id), ReadStatements.TopNodes(Organization), ReadStatements.Children(Organization, id),
            ReadStatements.AttributeValues(Organization, id), ReadStatements.StateChanges(Organization, id), ReadStatements.Requirements(Organization, id),
            ReadStatements.Reminders(Organization, id), ReadStatements.Revision(Organization), ReadStatements.LinksFrom(Organization, id),
            ReadStatements.LinksTo(Organization, LinkTargetKinds.Record, id),
        ];

        statements.ShouldAllBe(statement => statement.Text.Contains("OrganizationId = @OrganizationId", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RemovedLinksAreNotListed()
    {
        ReadStatements.LinksFrom(Organization, DocumentRecordsTestFixture.NewId()).Text.ShouldContain("RemovedAt IS NULL");
        ReadStatements.LinksTo(Organization, LinkTargetKinds.Document, DocumentRecordsTestFixture.NewId()).Text.ShouldContain("RemovedAt IS NULL");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OnlyDocumentAndRecordTargetsAreChecked()
    {
        ReadStatements.TargetFound(Organization, LinkTargetKinds.Document, DocumentRecordsTestFixture.NewId()).ShouldNotBeNull().Text.ShouldContain("doc.DocumentRecord");
        ReadStatements.TargetFound(Organization, LinkTargetKinds.Record, DocumentRecordsTestFixture.NewId()).ShouldNotBeNull().Text.ShouldContain("doc.RecordNode");
        ReadStatements.TargetFound(Organization, "Vehicle", DocumentRecordsTestFixture.NewId()).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void DefinitionsComeFromThePlatformTheTenantAndTheOrganization()
    {
        var statement = DefinitionStatements.DocumentTypes(DocumentRecordsTestFixture.NewId(), null);
        using var command = statement.Command();

        statement.Text.ShouldContain("(TenantId IS NULL AND OrganizationId IS NULL)");
        statement.Text.ShouldContain("(TenantId = @TenantId AND OrganizationId IS NULL)");
        statement.Text.ShouldContain("(@OrganizationId IS NOT NULL AND TenantId = @TenantId AND OrganizationId = @OrganizationId)");
        command.Parameters["@OrganizationId"].SqlDbType.ShouldBe(SqlDbType.UniqueIdentifier);
        command.Parameters["@OrganizationId"].Value.ShouldBe(DBNull.Value);
    }
}
