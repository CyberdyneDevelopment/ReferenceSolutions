using System;
using System.Data;
using System.Linq;
using FdwReferenceConnections.DocumentRecords.Rows;
using FdwReferenceConnections.DocumentRecords.Sql;

namespace FdwReferenceConnections.DocumentRecords.Tests;

/// <summary>
/// What SQL Server is sent for a write of an organization's records (FDW-3731): the entities it hangs off are found first, every value
/// travels as a parameter, ids as uniqueidentifier, and nothing is deleted. No SQL Server is involved: the statements are built and read.
/// </summary>
[Collection(nameof(DocumentRecordsTestCollection))]
public sealed class WriteStatementsTests
{
    private static readonly Guid Organization = DocumentRecordsTestFixture.NewId();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADocumentOnARecordFindsTheRecordBeforeItIsSaved()
    {
        var record = DocumentRecordsTestFixture.NewId();
        var document = new DocumentRecordRow(DocumentRecordsTestFixture.NewId(), Organization, "Permit", "Pole 12 permit", "Draft", null, DateTimeOffset.UtcNow, record);

        var statements = WriteStatements.SaveDocument(Organization, document);

        statements.Select(statement => statement.Name).ShouldBe(["FindRecord", "SaveDocumentRecord"]);
        statements[0].MustFind.ShouldBe("Record");
        statements[0].MustFindId.ShouldBe(record);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADocumentOnNoRecordIsSavedWithoutAFind()
    {
        var document = new DocumentRecordRow(DocumentRecordsTestFixture.NewId(), Organization, "Permit", "Permit", "Draft", null, DateTimeOffset.UtcNow, null);

        WriteStatements.SaveDocument(Organization, document).Select(statement => statement.Name).ShouldBe(["SaveDocumentRecord"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void EveryValueTravelsAsAParameterAndANullIsADatabaseNull()
    {
        var title = "x'; DROP TABLE doc.DocumentRecord; --";
        var document = new DocumentRecordRow(DocumentRecordsTestFixture.NewId(), Organization, "Permit", title, "Draft", null, DateTimeOffset.UtcNow, null);

        using var command = WriteStatements.SaveDocument(Organization, document).Single().Command();

        command.CommandText.ShouldNotContain(title);
        command.Parameters["@Title"].Value.ShouldBe(title);
        command.Parameters["@EffectiveDate"].Value.ShouldBe(DBNull.Value);
        command.Parameters["@RecordId"].SqlDbType.ShouldBe(SqlDbType.UniqueIdentifier);
        command.Parameters["@RecordId"].Value.ShouldBe(DBNull.Value);
        command.Parameters["@OrganizationId"].Value.ShouldBe(Organization);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void RemovingALinkKeepsItsRow()
    {
        var statements = WriteStatements.RemoveLink(Organization, DocumentRecordsTestFixture.NewId());

        statements.Select(statement => statement.Name).ShouldBe(["FindLink", "RemoveDocumentLink"]);
        statements[1].Text.ShouldContain("SET RemovedAt = sysdatetimeoffset()");
        statements.ShouldAllBe(statement => !statement.Text.Contains("DELETE", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void EnteringAStateAppendsTheChangeAndMovesTheDocument()
    {
        var change = new StateChangeRow(DocumentRecordsTestFixture.NewId(), DocumentRecordsTestFixture.NewId(), "Approved", DateTimeOffset.UtcNow, "afcadmin", null);

        var statements = WriteStatements.EnterState(Organization, change);

        statements.Select(statement => statement.Name).ShouldBe(["FindDocument", "EnterState"]);
        statements[1].Text.ShouldContain("INSERT INTO doc.StateChange");
        statements[1].Text.ShouldContain("UPDATE doc.DocumentRecord");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TheRevisionStartsAtOneUnderTheOrganizationsOwnId()
    {
        var statement = WriteStatements.StartRevision(Organization).Single();

        statement.Text.ShouldContain("SELECT @OrganizationId, @OrganizationId, 1");
        statement.Text.ShouldContain("WHERE NOT EXISTS");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AReminderIsRecordedOnlyWhenItsIdIsNew()
    {
        var entry = new ReminderEntryRow(DocumentRecordsTestFixture.NewId(), DocumentRecordsTestFixture.NewId(), DateTimeOffset.UtcNow, "records@example.com");

        var statements = WriteStatements.RecordReminder(Organization, entry);

        statements.Select(statement => statement.Name).ShouldBe(["FindRequirement", "RecordReminder"]);
        statements[1].Text.ShouldContain("WHERE NOT EXISTS (SELECT 1 FROM doc.ReminderEntry WHERE Id = @Id)");
    }
}
