using FluentValidation.TestHelper;
using Reference.Etl.Server.Endpoints;
using Reference.Etl.Server.Validators;
using Shouldly;
using Xunit;

namespace Reference.Etl.Server.Tests.Validators;

[Trait("Priority", "P1")]
[Trait("Category", "Etl")]
public sealed class TriggerJobRequestValidatorTests
{
    private readonly TriggerJobRequestValidator _sut = new();

    [Fact]
    public void ValidRequestPassesValidation()
    {
        // Arrange
        var request = new TriggerJobRequest { PipelineName = "TestPipeline" };

        // Act
        var result = _sut.TestValidate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void EmptyPipelineNameFailsValidation()
    {
        // Arrange
        var request = new TriggerJobRequest { PipelineName = string.Empty };

        // Act
        var result = _sut.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PipelineName);
    }

    [Fact]
    public void NullPipelineNameFailsValidation()
    {
        // Arrange
        var request = new TriggerJobRequest { PipelineName = null! };

        // Act
        var result = _sut.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PipelineName);
    }

    [Fact]
    public void WhitespaceOnlyPipelineNameFailsValidation()
    {
        // Arrange
        var request = new TriggerJobRequest { PipelineName = "   " };

        // Act
        var result = _sut.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PipelineName);
    }

    [Fact]
    public void ValidRequestWithOptionalFieldsPassesValidation()
    {
        // Arrange
        var request = new TriggerJobRequest
        {
            PipelineName = "TestPipeline",
            TriggerSource = "Manual",
            ScheduleName = "NightlySchedule"
        };

        // Act
        var result = _sut.TestValidate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ValidRequestWithNullOptionalFieldsPassesValidation()
    {
        // Arrange
        var request = new TriggerJobRequest
        {
            PipelineName = "TestPipeline",
            TriggerSource = null,
            ScheduleName = null
        };

        // Act
        var result = _sut.TestValidate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }
}
