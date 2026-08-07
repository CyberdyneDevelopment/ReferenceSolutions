using System;
using Fdw.Services.SecretManagers.EnvironmentVariable.Configuration;
using ReferenceSecretManagers.EnvironmentVariable.Handlers;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw;
using Fdw.Services;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.EnvironmentVariable.Commands;

namespace ReferenceSecretManagers.EnvironmentVariable.Tests;

/// <summary>
/// Tests for <see cref="EnvironmentVariableExecutionContext"/>: null guards and the
/// CaseSensitive-driven <see cref="StringComparison"/> selection used by the prefix-matching logic.
/// </summary>
public class EnvironmentVariableExecutionContextTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ConstructorNullLoggerThrowsArgumentNullException()
    {
        // Arrange
        var config = new EnvironmentVariableConfiguration();

        // Act
        var act = () => new EnvironmentVariableExecutionContext(null!, config, "svc-1");

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ConstructorNullConfigurationThrowsArgumentNullException()
    {
        // Act
        var act = () => new EnvironmentVariableExecutionContext(NullLogger.Instance, null!, "svc-1");

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ConstructorNullServiceIdThrowsArgumentNullException()
    {
        // Arrange
        var config = new EnvironmentVariableConfiguration();

        // Act
        var act = () => new EnvironmentVariableExecutionContext(NullLogger.Instance, config, null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void CaseSensitiveTrueUsesOrdinalComparison()
    {
        // Arrange
        var config = new EnvironmentVariableConfiguration { CaseSensitive = true };

        // Act
        var context = new EnvironmentVariableExecutionContext(NullLogger.Instance, config, "svc-1");

        // Assert
        context.StringComparison.ShouldBe(StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void CaseSensitiveFalseUsesOrdinalIgnoreCaseComparison()
    {
        // Arrange
        var config = new EnvironmentVariableConfiguration { CaseSensitive = false };

        // Act
        var context = new EnvironmentVariableExecutionContext(NullLogger.Instance, config, "svc-1");

        // Assert
        context.StringComparison.ShouldBe(StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void PropertiesExposeSuppliedValues()
    {
        // Arrange
        var config = new EnvironmentVariableConfiguration();

        // Act
        var context = new EnvironmentVariableExecutionContext(NullLogger.Instance, config, "svc-42");

        // Assert
        context.Logger.ShouldBe(NullLogger.Instance);
        context.ServiceId.ShouldBe("svc-42");
        context.EnvironmentVariableConfiguration.ShouldBeSameAs(config);
        context.Configuration.ShouldBeSameAs(config);
    }
}
