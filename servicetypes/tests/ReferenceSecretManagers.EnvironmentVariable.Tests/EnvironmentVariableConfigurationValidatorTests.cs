using Fdw.Services.SecretManagers.EnvironmentVariable.Configuration;
using Fdw;
using Fdw.Services;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.EnvironmentVariable.Commands;

namespace ReferenceSecretManagers.EnvironmentVariable.Tests;

/// <summary>
/// Tests for <see cref="EnvironmentVariableConfigurationValidator"/>: the required Prefix (the
/// FDW_SECRET_*-style guard against accidental env-var exposure) and Separator rules.
/// </summary>
public class EnvironmentVariableConfigurationValidatorTests
{
    private static EnvironmentVariableConfiguration MakeConfig(string prefix = "FDW_SECRET_", string separator = "__") => new()
    {
        Prefix = prefix,
        Separator = separator,
    };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidateValidConfigurationIsValid()
    {
        // Arrange
        var validator = new EnvironmentVariableConfigurationValidator();
        var config = MakeConfig();

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void ValidateMissingPrefixIsInvalid()
    {
        // Arrange — Why: a missing prefix would let ALL environment variables resolve as secrets;
        // this is the guard the no-fallback rule requires.
        var validator = new EnvironmentVariableConfigurationValidator();
        var config = MakeConfig(prefix: string.Empty);

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(EnvironmentVariableConfiguration.Prefix));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ValidateMissingSeparatorIsInvalid()
    {
        // Arrange
        var validator = new EnvironmentVariableConfigurationValidator();
        var config = MakeConfig(separator: string.Empty);

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(EnvironmentVariableConfiguration.Separator));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ValidateBothMissingReportsBothErrors()
    {
        // Arrange
        var validator = new EnvironmentVariableConfigurationValidator();
        var config = MakeConfig(prefix: string.Empty, separator: string.Empty);

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(2);
    }
}
