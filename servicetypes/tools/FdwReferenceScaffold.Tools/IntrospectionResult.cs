using System.Collections.Generic;
using FdwReferenceConfigurationSchema.Tools;

namespace FdwReferenceScaffold.Tools;

/// <summary>Outcome of attempting to connect to a database and read its schema for one DataStore.</summary>
internal sealed class IntrospectionResult
{
    public bool Succeeded { get; private init; }
    public string SkipReason { get; private init; } = string.Empty;
    public List<SchemaPath> Paths { get; private init; } = [];

    public static IntrospectionResult Success(List<SchemaPath> paths) => new() { Succeeded = true, Paths = paths };

    public static IntrospectionResult Skipped(string reason) => new() { Succeeded = false, SkipReason = reason };
}
