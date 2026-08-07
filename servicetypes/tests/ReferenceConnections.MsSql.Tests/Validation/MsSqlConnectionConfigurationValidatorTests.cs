using FluentValidation.TestHelper;
using Fdw.Services.Connections.MsSql;
using Fdw.Services.Connections.MsSql.Validation;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace ReferenceConnections.MsSql.Tests.Validation;

public sealed class MsSqlConnectionConfigurationValidatorTests
{
    private readonly MsSqlConnectionConfigurationValidator _validator = new();

    // Why: Name is a header field on ConnectionConfiguration after config-split.
    // MsSqlConnectionConfiguration exposes Name as an explicit IGenericConfiguration member
    // returning string.Empty. Name validation has been removed from the typed-body validator.

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidatePassesWithValidConfiguration()
    {
        // Arrange
        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDatabase",
            Port = 1433,
            CommandTimeoutSeconds = 30,
            ConnectionTimeoutSeconds = 15,
            EnableConnectionPooling = true,
            MaxPoolSize = 100,
            MinPoolSize = 0,
            DefaultSchema = "dbo"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateFailsWithEmptyServer()
    {
        // Arrange
        var config = new MsSqlConnectionConfiguration
        {
            Server = string.Empty,
            Database = "TestDatabase",
            DefaultSchema = "dbo"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Server);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateFailsWithEmptyDatabase()
    {
        // Arrange
        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = string.Empty,
            DefaultSchema = "dbo"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Database);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateFailsWithPortBelowRange()
    {
        // Arrange
        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDatabase",
            Port = 0,
            DefaultSchema = "dbo"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Port);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateFailsWithPortAboveRange()
    {
        // Arrange
        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDatabase",
            Port = 65536,
            DefaultSchema = "dbo"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Port);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(1)]
    [InlineData(65535)]
    public void ValidatePassesWithPortAtBoundaries(int port)
    {
        // Arrange
        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDatabase",
            Port = port,
            DefaultSchema = "dbo"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Port);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateFailsWithZeroCommandTimeout()
    {
        // Arrange
        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDatabase",
            CommandTimeoutSeconds = 0,
            DefaultSchema = "dbo"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CommandTimeoutSeconds);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateFailsWithZeroConnectionTimeout()
    {
        // Arrange
        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDatabase",
            ConnectionTimeoutSeconds = 0,
            DefaultSchema = "dbo"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConnectionTimeoutSeconds);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidatePassesWhenPoolingDisabledWithAnyPoolSize()
    {
        // Arrange
        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDatabase",
            EnableConnectionPooling = false,
            MaxPoolSize = 0,
            MinPoolSize = 0,
            DefaultSchema = "dbo"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MaxPoolSize);
        result.ShouldNotHaveValidationErrorFor(x => x.MinPoolSize);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateFailsWhenPoolingEnabledWithZeroMaxPoolSize()
    {
        // Arrange
        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDatabase",
            EnableConnectionPooling = true,
            MaxPoolSize = 0,
            MinPoolSize = 0,
            DefaultSchema = "dbo"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxPoolSize);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateFailsWhenMinPoolSizeExceedsMaxPoolSize()
    {
        // Arrange
        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDatabase",
            EnableConnectionPooling = true,
            MaxPoolSize = 100,
            MinPoolSize = 200,
            DefaultSchema = "dbo"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MinPoolSize);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidatePassesWhenMinPoolSizeEqualsMaxPoolSize()
    {
        // Arrange
        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDatabase",
            EnableConnectionPooling = true,
            MaxPoolSize = 50,
            MinPoolSize = 50,
            DefaultSchema = "dbo"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MinPoolSize);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateFailsWithEmptyDefaultSchema()
    {
        // Arrange
        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDatabase",
            DefaultSchema = string.Empty
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DefaultSchema);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateOptionsReturnsSuccessForValidConfig()
    {
        // Arrange
        var config = new MsSqlConnectionConfiguration
        {
            Server = "localhost",
            Database = "TestDatabase",
            DefaultSchema = "dbo"
        };

        // Act
        var result = _validator.Validate(Options.DefaultName, config);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateOptionsReturnsFailureForInvalidConfig()
    {
        // Arrange
        var config = new MsSqlConnectionConfiguration
        {
            Server = string.Empty,
            Database = string.Empty,
            DefaultSchema = string.Empty
        };

        // Act
        var result = _validator.Validate(Options.DefaultName, config);

        // Assert
        result.Failed.ShouldBeTrue();
    }
}
