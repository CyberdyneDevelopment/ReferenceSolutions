using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;

namespace FdwReferenceConnections.FileSystem.Tests;

/// <summary>
/// Builds minimal mocked <see cref="IDataCommand"/> values for the FileSystem record round-trip tests:
/// a Query command (read) and an Insert command (write) carrying the rows as its input data.
/// </summary>
internal static class CommandStub
{
    public static IDataCommand Query()
    {
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("Query");
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        return command.Object;
    }

    /// <summary>
    /// A Query command carrying an ordering and/or a page. Mocks <see cref="IQueryCommand"/> rather
    /// than <see cref="IDataCommand"/> because the translator reads these through an
    /// <c>as IQueryCommand</c> cast — a plain IDataCommand casts to null and the query silently
    /// becomes unordered and unpaged, which is the defect these stubs exist to catch.
    /// </summary>
    public static IDataCommand Query(IOrderingExpression? ordering, IPagingExpression? paging)
    {
        var command = new Mock<IQueryCommand>();
        command.Setup(c => c.CommandType).Returns("Query");
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        command.Setup(c => c.Filter).Returns((IFilterExpression?)null);
        command.Setup(c => c.Joins).Returns([]);
        command.Setup(c => c.Ordering).Returns(ordering);
        command.Setup(c => c.Paging).Returns(paging);
        return command.Object;
    }

    public static IDataCommand Insert(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var command = new Mock<IDataCommandWithInput>();
        command.Setup(c => c.CommandType).Returns("Insert");
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        command.Setup(c => c.InputData).Returns(rows);
        return command.Object;
    }
}
