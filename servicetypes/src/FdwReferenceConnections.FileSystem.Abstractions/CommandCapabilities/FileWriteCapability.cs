using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Abstractions;

namespace FdwReferenceConnections.FileSystem.Abstractions.CommandCapabilities;

/// <summary>
/// File write capability — writes content to a file accessible through the connection.
/// Used by file-system connection types that support write operations.
/// </summary>
/// <remarks>
/// Configuration keys:
/// <list type="bullet">
///   <item><c>Path</c> — file path relative to the connection's root (required).</item>
///   <item><c>Format</c> — file format: text, json, or csv (required).</item>
///   <item><c>Encoding</c> — text encoding; defaults to utf-8 at runtime if not set.</item>
///   <item><c>Overwrite</c> — whether to overwrite an existing file (default true).</item>
/// </list>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CommandCapabilityTypes), "FileWrite", RestrictToCurrentCompilation = true)]
public sealed class FileWriteCapability : CommandCapabilityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileWriteCapability"/> class.
    /// </summary>
    public FileWriteCapability()
        : base(
            name: "FileWrite",
            displayName: "File Write",
            configurationFields:
            [
                new ConfigurationFieldDescriptor(
                    Key: "Path",
                    Label: "File Path",
                    Placeholder: "data/output/results.csv",
                    InputKind: ConfigurationFieldKinds.Text,
                    IsRequired: true),
                new ConfigurationFieldDescriptor(
                    Key: "Format",
                    Label: "Format",
                    Placeholder: string.Empty,
                    InputKind: ConfigurationFieldKinds.Select,
                    SelectOptions: ["text:Text", "json:JSON", "csv:CSV"],
                    IsRequired: true),
                new ConfigurationFieldDescriptor(
                    Key: "Encoding",
                    Label: "Encoding",
                    Placeholder: string.Empty,
                    InputKind: ConfigurationFieldKinds.Select,
                    SelectOptions: ["utf-8:UTF-8", "utf-16:UTF-16", "ascii:ASCII", "latin-1:Latin-1"]),
                new ConfigurationFieldDescriptor(
                    Key: "Overwrite",
                    Label: "Overwrite Existing",
                    Placeholder: string.Empty,
                    InputKind: ConfigurationFieldKinds.Boolean),
            ])
    {
    }
}
