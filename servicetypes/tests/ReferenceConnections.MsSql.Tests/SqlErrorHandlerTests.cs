using Fdw.Services.Connections.MsSql.ErrorHandlers;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ReferenceConnections.MsSql.Tests;

/// <summary>
/// Tests for SqlErrorHandlers TypeCollection dispatch.
/// Verifies that ByErrorNumber returns the correct handler for each SQL Server error number,
/// IsRetryable is correct per handler, and CreateFailureMessage returns non-null.
/// </summary>
[Collection(nameof(SqlErrorHandlerTestCollection))]
public class SqlErrorHandlerTests
{
    #region ByErrorNumber Dispatch

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "MsSql")]
    public void ByErrorNumber_229_ReturnsPermissionDeniedHandler()
    {
        // Act
        var handler = SqlErrorHandlers.ByErrorNumber(229);

        // Assert
        handler.ShouldNotBeNull();
        handler.Name.ShouldBe("PermissionDenied");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "MsSql")]
    public void ByErrorNumber_208_ReturnsObjectNotFoundHandler()
    {
        // Act
        var handler = SqlErrorHandlers.ByErrorNumber(208);

        // Assert
        handler.ShouldNotBeNull();
        handler.Name.ShouldBe("ObjectNotFound");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "MsSql")]
    public void ByErrorNumber_18456_ReturnsLoginFailedHandler()
    {
        // Act
        var handler = SqlErrorHandlers.ByErrorNumber(18456);

        // Assert
        handler.ShouldNotBeNull();
        handler.Name.ShouldBe("LoginFailed");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "MsSql")]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(53)]
    public void ByErrorNumber_ConnectionFailedNumbers_AllReturnConnectionFailedHandler(int errorNumber)
    {
        // Act
        var handler = SqlErrorHandlers.ByErrorNumber(errorNumber);

        // Assert
        handler.ShouldNotBeNull();
        handler.Name.ShouldBe("ConnectionFailed");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "MsSql")]
    public void ByErrorNumber_1205_ReturnsDeadlockHandler()
    {
        // Act
        var handler = SqlErrorHandlers.ByErrorNumber(1205);

        // Assert
        handler.ShouldNotBeNull();
        handler.Name.ShouldBe("Deadlock");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "MsSql")]
    public void ByErrorNumber_Minus2_ReturnsQueryTimeoutHandler()
    {
        // Act
        var handler = SqlErrorHandlers.ByErrorNumber(-2);

        // Assert
        handler.ShouldNotBeNull();
        handler.Name.ShouldBe("QueryTimeout");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "MsSql")]
    public void ByErrorNumber_UnknownNumber_ReturnsNotFoundSentinel()
    {
        // Act
        var handler = SqlErrorHandlers.ByErrorNumber(99999);

        // Assert
        handler.ShouldNotBeNull();
        handler.ShouldBe(SqlErrorHandlers.NotFound);
    }

    #endregion

    #region IsRetryable

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "MsSql")]
    [InlineData(229, false)]   // PermissionDenied
    [InlineData(208, false)]   // ObjectNotFound
    [InlineData(18456, false)] // LoginFailed
    [InlineData(-1, true)]     // ConnectionFailed
    [InlineData(1205, true)]   // Deadlock
    [InlineData(-2, true)]     // QueryTimeout
    public void IsRetryable_ReturnsCorrectValue(int errorNumber, bool expectedRetryable)
    {
        // Act
        var handler = SqlErrorHandlers.ByErrorNumber(errorNumber);

        // Assert
        handler.IsRetryable.ShouldBe(expectedRetryable);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "MsSql")]
    public void NotFoundSentinel_IsNotRetryable()
    {
        // Act
        var handler = SqlErrorHandlers.ByErrorNumber(99999);

        // Assert
        handler.IsRetryable.ShouldBeFalse();
    }

    #endregion

    #region CreateFailureMessage

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "MsSql")]
    [InlineData(229)]
    [InlineData(208)]
    [InlineData(18456)]
    [InlineData(-1)]
    [InlineData(1205)]
    [InlineData(-2)]
    public void CreateFailureMessage_ReturnsNonNull(int errorNumber)
    {
        // Arrange
        var handler = SqlErrorHandlers.ByErrorNumber(errorNumber);
        var logger = NullLogger.Instance;
        var exception = new InvalidOperationException("Test exception");

        // Act
        var message = handler.CreateFailureMessage(logger, exception, "SELECT 1");

        // Assert
        message.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "MsSql")]
    public void CreateFailureMessage_NotFoundSentinel_ReturnsNonNull()
    {
        // Arrange
        var handler = SqlErrorHandlers.NotFound;
        var logger = NullLogger.Instance;
        var exception = new InvalidOperationException("Unknown SQL error");

        // Act
        var message = handler.CreateFailureMessage(logger, exception, "SELECT 1");

        // Assert
        message.ShouldNotBeNull();
    }

    #endregion

    #region TypeCollection Contracts

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "MsSql")]
    public void All_Returns6Handlers()
    {
        // Act
        var all = SqlErrorHandlers.All();

        // Assert -- 6 registered handlers
        all.Count().ShouldBe(6);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "MsSql")]
    public void ByName_PermissionDenied_ReturnsCorrectHandler()
    {
        // Act
        var handler = SqlErrorHandlers.ByName("PermissionDenied");

        // Assert
        handler.ShouldNotBeNull();
        handler.SqlErrorNumbers.ShouldContain(229);
    }

    #endregion
}

/// <summary>
/// Fixture that ensures SqlErrorHandlers TypeCollection is fully initialized
/// before any tests run.
/// </summary>
public sealed class SqlErrorHandlerFixture
{
    public SqlErrorHandlerFixture()
    {
        _ = SqlErrorHandlers.All();
    }
}

[CollectionDefinition(nameof(SqlErrorHandlerTestCollection))]
public sealed class SqlErrorHandlerTestCollection : ICollectionFixture<SqlErrorHandlerFixture>
{
}
