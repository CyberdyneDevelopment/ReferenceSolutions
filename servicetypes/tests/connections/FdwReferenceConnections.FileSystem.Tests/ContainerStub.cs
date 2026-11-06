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

        // The family and the separator come from the real format of that name: the connector finds its reader and writer by
        // the family (FDW-1315), so a mock that stated only a name would be read by no reader.
        var realFormat = FormatTypes.ByName(format);
        var formatType = new Mock<IFormatType>();
        formatType.Setup(f => f.Name).Returns(format);
        formatType.Setup(f => f.Family).Returns(realFormat.Family);
        formatType.Setup(f => f.Separator).Returns(realFormat.Separator);

        var path = new Mock<IPath>();
        path.Setup(p => p.Domain).Returns("File");
        path.Setup(p => p.PathValue).Returns(fileName);

        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(fields.Cast<IField>().ToList());
        schema.Setup(s => s.GetReturnedFields(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns(fields.Cast<IField>().ToList());

        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns("TestContainer");
        // IDataContainer redeclares Name (new string Name), so the IStorageContainer.Name the row formats read (RowFormatChoices owner) needs its own setup.
        container.As<IStorageContainer>().Setup(c => c.Name).Returns("TestContainer");
        container.Setup(c => c.Format).Returns(formatType.Object);
        container.Setup(c => c.Metadata).Returns(Stated(realFormat, metadata));
        container.Setup(c => c.Path).Returns(path.Object);
        container.Setup(c => c.Schema).Returns(schema.Object);
        container.Setup(c => c.Nodes).Returns(fields.Cast<IDataNode>().ToList());
        container.Setup(c => c.Keys).Returns(new List<IContainerKey>());
        return container.Object;
    }

    // Every choice the readers and writers read, as a configured container states them (FDW-3823: a format takes no choice by default),
    // with the values the options carried before choices had to be stated. What a test states itself replaces the value here.
    private static Dictionary<string, object> Stated(IFormatType format, IReadOnlyDictionary<string, object> metadata)
    {
        var stated = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["ContinueOnError"] = true,
            ["PropertyNameCaseInsensitive"] = true,
            ["FlattenNestedObjects"] = false,
            ["SkipValidation"] = false,
            ["UseElementContent"] = true,
            ["IncludeAttributes"] = true,
            ["RootElementName"] = "Rows",
            ["RowElementName"] = "Row",
            ["OmitXmlDeclaration"] = true,
            ["HasHeader"] = false,
            ["Separator"] = string.IsNullOrEmpty(format.Separator) ? "," : format.Separator,
            ["ContainsQuotedFields"] = true,
            ["Trim"] = string.Equals(format.Family, "FixedWidth", StringComparison.Ordinal),
            ["QuoteChar"] = "\"",
            ["WriteByteOrderMark"] = false,
            ["NeutralizeFormulas"] = false,
            ["SheetName"] = "Data",
        };
        foreach (var pair in metadata)
            stated[pair.Key] = pair.Value;
        return stated;
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
        var text = new Mock<IFieldType>();
        text.Setup(f => f.ClrType).Returns(typeof(string));
        asField.Setup(f => f.FieldType).Returns(text.Object);

        return field.Object;
    }
}
