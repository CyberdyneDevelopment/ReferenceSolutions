using Fdw.Data.Abstractions;

namespace FdwReferenceConnections.FileSystem.Tests;

/// <summary>
/// Builds a minimal mocked <see cref="IDataContainer"/> for the FileSystem record round-trip tests: a
/// configured container with a format name, metadata bag, a file physical path, and a field schema whose
/// children are both <see cref="IDataField"/> (tree nodes) and <see cref="IField"/> (schema projection).
/// The configured fields ARE the type — no compile-time DTO.
/// </summary>
internal static class ContainerStub
{
    public static IDataContainer Build(
        string fileName,
        string format,
        IReadOnlyList<string> fieldNames,
        IReadOnlyDictionary<string, object> metadata,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>>? fieldMetadata)
    {
        var fields = fieldNames.Select(n => BuildField(n, fieldMetadata)).ToList();

        var formatType = new Mock<IFormatType>();
        formatType.Setup(f => f.Name).Returns(format);

        var path = new Mock<IPath>();
        path.Setup(p => p.Domain).Returns("File");
        path.Setup(p => p.PathValue).Returns(fileName);

        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(fields.Cast<IField>().ToList());
        schema.Setup(s => s.GetReturnedFields(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns(fields.Cast<IField>().ToList());

        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns("TestContainer");
        container.Setup(c => c.Format).Returns(formatType.Object);
        container.Setup(c => c.Metadata).Returns(metadata);
        container.Setup(c => c.Path).Returns(path.Object);
        container.Setup(c => c.Schema).Returns(schema.Object);
        container.Setup(c => c.Nodes).Returns(fields.Cast<IDataNode>().ToList());
        return container.Object;
    }

    // Why: each field is mocked as both IDataField (the container tree child the connector reads via
    // Nodes) and IField (the schema projection the options builder reads via Schema.Fields). One object
    // backs both views so the field name + metadata are identical across them.
    private static IDataField BuildField(
        string name,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>>? fieldMetadata)
    {
        IReadOnlyDictionary<string, object>? meta = null;
        fieldMetadata?.TryGetValue(name, out meta);

        var field = new Mock<IDataField>();
        field.Setup(f => f.Name).Returns(name);

        var asField = field.As<IField>();
        asField.Setup(f => f.Name).Returns(name);
        asField.Setup(f => f.Metadata).Returns(meta);

        return field.Object;
    }
}
