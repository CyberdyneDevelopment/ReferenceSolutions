using Reference.Etl.Server.Services.JobTriggerSources;
using Reference.Etl.Server.Services.JobTriggerSources.Options;
using Shouldly;
using Xunit;

namespace Reference.Etl.Server.Tests.TypeCollections;

/// <summary>
/// Tests for the JobTriggerSourceTypes TypeCollection.
/// These tests verify that all trigger source types are registered correctly
/// and accessible via name and ID lookups.
/// </summary>
/// <remarks>
/// TypeCollection tests use a shared fixture to ensure the source-generated
/// module initializer has run before any ByName/ById lookups.
/// </remarks>
[Collection("TypeCollection")]
[Trait("Priority", "P1")]
[Trait("Category", "Etl")]
public sealed class JobTriggerSourceTypesTests
{
    [Fact]
    public void AllReturnsAllFourTriggerSources()
    {
        // Arrange & Act
        var all = JobTriggerSourceTypes.All();

        // Assert
        all.Count.ShouldBe(4);
    }

    [Fact]
    public void ByNameReturnsApiTriggerSource()
    {
        // Arrange & Act
        var result = JobTriggerSourceTypes.ByName("Api");

        // Assert
        result.ShouldBeOfType<ApiJobTriggerSource>();
        result.Id.ShouldBe(4);
    }

    [Fact]
    public void ByNameReturnsEventTriggerSource()
    {
        // Arrange & Act
        var result = JobTriggerSourceTypes.ByName("Event");

        // Assert
        result.ShouldBeOfType<EventJobTriggerSource>();
        result.Id.ShouldBe(3);
    }

    [Fact]
    public void ByNameReturnsManualTriggerSource()
    {
        // Arrange & Act
        var result = JobTriggerSourceTypes.ByName("Manual");

        // Assert
        result.ShouldBeOfType<ManualJobTriggerSource>();
        result.Id.ShouldBe(1);
    }

    [Fact]
    public void ByNameReturnsScheduledTriggerSource()
    {
        // Arrange & Act
        var result = JobTriggerSourceTypes.ByName("Scheduled");

        // Assert
        result.ShouldBeOfType<ScheduledJobTriggerSource>();
        result.Id.ShouldBe(2);
    }

    [Fact]
    public void ByIdReturnsCorrectType()
    {
        // Arrange & Act
        var result = JobTriggerSourceTypes.ById(1);

        // Assert
        result.ShouldBeOfType<ManualJobTriggerSource>();
    }

    [Fact]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        // Arrange & Act
        var result = JobTriggerSourceTypes.ByName("Unknown");

        // Assert
        result.ShouldBeSameAs(JobTriggerSourceTypes.NotFound);
    }
}
