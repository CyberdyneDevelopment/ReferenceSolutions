using System.Linq;
using System.Text.Json;
using Fdw.Messages;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceConnections.Sqlite.Tests;

/// <summary>
/// A SQLite connection type attaches its configuration validator, so a create or update that states no data source is refused
/// before it is saved, with the rule's message shown to the caller (FDW-3541). The validator existed and ran nowhere.
/// </summary>
public class SqliteConnectionValidationTests
{
    private static JsonElement Json(string text)
    {
        using var document = JsonDocument.Parse(text);
        return document.RootElement.Clone();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ACreateThatStatesNoDataSourceIsRefusedAndNamesTheMember()
    {
        var bound = new SqliteConnectionType().EndpointBehavior.Bind(Json("""{ "dataSource": "" }"""), NullLogger.Instance);

        bound.IsFailure.ShouldBeTrue();
        bound.Messages.OfType<IClientMessage>().ShouldHaveSingleItem().ClientText.ShouldContain("DataSource is required");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ACreateThatStatesADataSourceIsAccepted()
    {
        // The body Bind is given is the create's after the type's DeclaredDefaults fill Mode and Cache; the caller states its authentication.
        var bound = new SqliteConnectionType().EndpointBehavior.Bind(
            Json("""{ "dataSource": "/data/app.db", "mode": "ReadWriteCreate", "cache": "Default", "authenticationType": "None" }"""), NullLogger.Instance);

        bound.IsSuccess.ShouldBeTrue();
        bound.Value.ShouldBeOfType<SqliteConnectionConfiguration>().DataSource.ShouldBe("/data/app.db");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ACreateThatStatesANonPositiveTimeoutIsRefused()
    {
        var bound = new SqliteConnectionType().EndpointBehavior.Bind(
            Json("""{ "dataSource": "/data/app.db", "defaultTimeout": 0 }"""), NullLogger.Instance);

        bound.IsFailure.ShouldBeTrue();
        bound.Messages.OfType<IClientMessage>().ShouldHaveSingleItem().ClientText.ShouldContain("DefaultTimeout must be greater than 0");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AnAuthenticationKeyTheMethodDoesNotDeclareIsRefusedNamingTheKeyAndNotItsValue()
    {
        // FDW-4607: the key was never validated, so it is not known to be a name; its value must not reach the refusal.
        var bound = new SqliteConnectionType().EndpointBehavior.Bind(
            Json("""{ "dataSource": "/data/app.db", "mode": "ReadWriteCreate", "cache": "Default", "authenticationType": "None", "additionalProperties": { "Password": "not-a-real-password" } }"""),
            NullLogger.Instance);

        bound.IsFailure.ShouldBeTrue();
        var text = bound.Messages.OfType<IClientMessage>().ShouldHaveSingleItem().ClientText;
        text.ShouldContain("Password");
        text.ShouldNotContain("not-a-real-password");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TheAuthenticationKeysTheMethodDeclaresAreAccepted()
    {
        var bound = new SqliteConnectionType().EndpointBehavior.Bind(
            Json("""{ "dataSource": "/data/app.db", "mode": "ReadWriteCreate", "cache": "Default", "authenticationType": "EncryptionKey", "additionalProperties": { "SecretKeyName": "app-db-key", "SecretManagerName": "CompanyVault" } }"""),
            NullLogger.Instance);

        bound.IsSuccess.ShouldBeTrue();
    }
}
