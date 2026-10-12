using Fdw.Services.Connections.MsSql.ErrorHandlers;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceConnections.MsSql.Tests;

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

    #region CreateFailureMessages

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "MsSql")]
    [InlineData(229)]
    [InlineData(208)]
    [InlineData(18456)]
    [InlineData(1205)]
    [InlineData(-2)]
    public void CreateFailureMessagesForAMappedErrorIsTheHandlersOneMessage(int errorNumber)
    {
        // Arrange: an error a handler was written for says all there is to say in its own message
        var handler = SqlErrorHandlers.ByErrorNumber(errorNumber);
        var exception = new InvalidOperationException("outer", new InvalidOperationException("inner"));

        // Act
        var messages = handler.CreateFailureMessages(NullLogger.Instance, exception, "SELECT 1");

        // Assert
        messages.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "MsSql")]
    public void CreateFailureMessagesForAnUnmappedErrorCarriesTheFlattenedExceptionChain()
    {
        // Arrange: no handler was written for this error, so it is a generic SQL failure
        var handler = SqlErrorHandlers.ByErrorNumber(99999);
        var exception = new InvalidOperationException("outer", new InvalidOperationException("inner"));

        // Act
        var messages = handler.CreateFailureMessages(NullLogger.Instance, exception, "SELECT 1");

        // Assert: the structured message first, then one for each exception in the chain, outermost first
        messages.Count.ShouldBe(3);
        messages[1].Message.ShouldBe("outer");
        messages[2].Message.ShouldBe("inner");
    }

    #endregion

    #region TypeCollection Contracts

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
