using System.Text.Json;
using Fdw.Messages;
using FdwReferenceConnections.FileSystem.Registration;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceConnections.FileSystem.Tests;

/// <summary>
/// A file-system connection type attaches its configuration validator, so a create or update that states no root folder is refused
/// before it is saved, with the rule's message shown to the caller (FDW-3541). The validator existed and ran nowhere.
/// </summary>
public class FileSystemConnectionValidationTests
{
    private static JsonElement Json(string text)
    {
        using var document = JsonDocument.Parse(text);
        return document.RootElement.Clone();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ACreateThatStatesNoRootIsRefusedAndNamesTheMember()
    {
        var bound = new FileSystemConnectionType().EndpointBehavior.Bind(Json("""{ "root": "" }"""), NullLogger.Instance);

        bound.IsFailure.ShouldBeTrue();
        bound.Messages.OfType<IClientMessage>().ShouldHaveSingleItem().ClientText.ShouldContain("Root is required");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ACreateThatStatesARootIsAccepted()
    {
        var bound = new FileSystemConnectionType().EndpointBehavior.Bind(Json("""{ "root": "/data/files" }"""), NullLogger.Instance);

        bound.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TheDeprecatedBaseUrlFieldStillSetsTheRoot()
    {
        var bound = new FileSystemConnectionType().EndpointBehavior.Bind(Json("""{ "baseUrl": "/data/files" }"""), NullLogger.Instance);

        bound.IsSuccess.ShouldBeTrue();
        bound.Value.ShouldBeOfType<FileSystemConnectionConfiguration>().Root.ShouldBe("/data/files");
    }
}
