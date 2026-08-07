using Fdw.Messages;
using Fdw.Services.Connections.MsSql.Messages;

namespace ReferenceConnections.MsSql.Tests.Messages;

public class ConnectionFailedWithDetailsMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsId()
    {
        var msg = new ConnectionFailedWithDetailsMessage();

        msg.Id.ShouldBe(2001);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsName()
    {
        var msg = new ConnectionFailedWithDetailsMessage();

        msg.Name.ShouldBe("ConnectionFailedWithDetails");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsSeverityToError()
    {
        var msg = new ConnectionFailedWithDetailsMessage();

        msg.Severity.ShouldBe(MessageSeverity.Error);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsMessageTemplate()
    {
        var msg = new ConnectionFailedWithDetailsMessage();

        msg.Message.ShouldBe("SQL Server connection failed: {0}");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsCode()
    {
        var msg = new ConnectionFailedWithDetailsMessage();

        msg.Code.ShouldBe("MSSQL_CONN_FAILED");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SourceIsConnections()
    {
        var msg = new ConnectionFailedWithDetailsMessage();

        msg.Source.ShouldBe("Connections");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatInsertsArguments()
    {
        var msg = new ConnectionFailedWithDetailsMessage();

        var formatted = msg.Format("timeout after 30s");

        formatted.ShouldBe("SQL Server connection failed: timeout after 30s");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatWithNoArgsReturnsTemplate()
    {
        var msg = new ConnectionFailedWithDetailsMessage();

        var formatted = msg.Format();

        formatted.ShouldBe("SQL Server connection failed: {0}");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CategoryIsMessage()
    {
        var msg = new ConnectionFailedWithDetailsMessage();

        msg.Category.ShouldBe("Message");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OriginatedInIsConnections()
    {
        var msg = new ConnectionFailedWithDetailsMessage();

        msg.OriginatedIn.ShouldBe("Connections");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TimestampIsSetToUtcNow()
    {
        var before = DateTime.UtcNow;
        var msg = new ConnectionFailedWithDetailsMessage();
        var after = DateTime.UtcNow;

        msg.Timestamp.ShouldBeGreaterThanOrEqualTo(before);
        msg.Timestamp.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DetailsIsNull()
    {
        var msg = new ConnectionFailedWithDetailsMessage();

        msg.Details.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DataIsNull()
    {
        var msg = new ConnectionFailedWithDetailsMessage();

        msg.Data.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToStringContainsSeverityAndCode()
    {
        var msg = new ConnectionFailedWithDetailsMessage();

        var text = msg.ToString();

        text.ShouldContain("[Error]");
        text.ShouldContain("(MSSQL_CONN_FAILED)");
        text.ShouldContain("SQL Server connection failed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualsReturnsTrueForSameType()
    {
        var msg1 = new ConnectionFailedWithDetailsMessage();
        var msg2 = new ConnectionFailedWithDetailsMessage();

        msg1.Equals(msg2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualsReturnsFalseForDifferentType()
    {
        var msg1 = new ConnectionFailedWithDetailsMessage();
        var msg2 = new SqlExecutionFailedMessage();

        msg1.Equals(msg2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetHashCodeIsConsistent()
    {
        var msg1 = new ConnectionFailedWithDetailsMessage();
        var msg2 = new ConnectionFailedWithDetailsMessage();

        msg1.GetHashCode().ShouldBe(msg2.GetHashCode());
    }
}

public class SqlExecutionFailedMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsId()
    {
        var msg = new SqlExecutionFailedMessage();

        msg.Id.ShouldBe(2002);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsName()
    {
        var msg = new SqlExecutionFailedMessage();

        msg.Name.ShouldBe("SqlExecutionFailed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsSeverityToError()
    {
        var msg = new SqlExecutionFailedMessage();

        msg.Severity.ShouldBe(MessageSeverity.Error);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsMessageTemplate()
    {
        var msg = new SqlExecutionFailedMessage();

        msg.Message.ShouldBe("SQL execution failed: {0}");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsCode()
    {
        var msg = new SqlExecutionFailedMessage();

        msg.Code.ShouldBe("MSSQL_EXEC_FAILED");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SourceIsConnections()
    {
        var msg = new SqlExecutionFailedMessage();

        msg.Source.ShouldBe("Connections");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatInsertsArguments()
    {
        var msg = new SqlExecutionFailedMessage();

        var formatted = msg.Format("invalid syntax near '('");

        formatted.ShouldBe("SQL execution failed: invalid syntax near '('");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToStringContainsSeverityAndCode()
    {
        var msg = new SqlExecutionFailedMessage();

        var text = msg.ToString();

        text.ShouldContain("[Error]");
        text.ShouldContain("(MSSQL_EXEC_FAILED)");
        text.ShouldContain("SQL execution failed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualsReturnsTrueForSameType()
    {
        var msg1 = new SqlExecutionFailedMessage();
        var msg2 = new SqlExecutionFailedMessage();

        msg1.Equals(msg2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetHashCodeIsConsistent()
    {
        var msg1 = new SqlExecutionFailedMessage();
        var msg2 = new SqlExecutionFailedMessage();

        msg1.GetHashCode().ShouldBe(msg2.GetHashCode());
    }
}

public class MsSqlConnectionMessageBaseTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DerivedClassInheritsFromConnectionMessage()
    {
        var msg = new ConnectionFailedWithDetailsMessage();

        msg.ShouldBeAssignableTo<MsSqlConnectionMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualsReturnsFalseForNull()
    {
        var msg = new ConnectionFailedWithDetailsMessage();

        msg.Equals(null).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualsReturnsFalseForNonMessageObject()
    {
        var msg = new ConnectionFailedWithDetailsMessage();

        msg.Equals("not a message").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TwoDistinctMessageTypesAreNotEqual()
    {
        var connFailed = new ConnectionFailedWithDetailsMessage();
        var execFailed = new SqlExecutionFailedMessage();

        connFailed.Equals(execFailed).ShouldBeFalse();
        execFailed.Equals(connFailed).ShouldBeFalse();
    }
}
