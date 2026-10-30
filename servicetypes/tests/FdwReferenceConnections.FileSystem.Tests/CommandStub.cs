using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;

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

    /// <summary>A Delete command with no filter, which removes every row of the file: what a truncate before a load builds.</summary>
    public static IDataCommand Delete()
    {
        var command = new Mock<IFilterableCommand>();
        command.Setup(c => c.CommandType).Returns("Delete");
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        command.Setup(c => c.Filter).Returns((IFilterExpression?)null);
        return command.Object;
    }

    /// <summary>An Insert command whose rows are mutable name→value dictionaries, the shape a pipeline's load sends.</summary>
    public static IDataCommand InsertMutable(IReadOnlyList<IDictionary<string, object?>> rows)
    {
        var command = new Mock<IDataCommandWithInput>();
        command.Setup(c => c.CommandType).Returns("Insert");
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        command.Setup(c => c.InputData).Returns(rows.ToList());
        return command.Object;
    }

    public static IDataCommand OfType(string commandType)
    {
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns(commandType);
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        return command.Object;
    }

    public static IDataCommand Expandable(string commandType, params IDataCommand[] steps)
    {
        var command = new Mock<IExpandableDataCommand>();
        command.Setup(c => c.CommandType).Returns(commandType);
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        command.Setup(c => c.Expand(It.IsAny<IStorageContainer>()))
            .Returns(GenericResult<IReadOnlyList<ExpandedCommand>>.Success(steps.Select(s => new ExpandedCommand(s)).ToList()));
        return command.Object;
    }
}
