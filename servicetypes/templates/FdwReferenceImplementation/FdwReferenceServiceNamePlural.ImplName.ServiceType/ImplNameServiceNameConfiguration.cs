using System;
using Fdw.Configuration;

namespace FdwReferenceServiceNamePlural.ImplName;

/// <summary>
/// Configuration for the ImplName ServiceName.
/// </summary>
// Why: in FDW proper this is the implementation configuration of its domain — it implements the
// domain's own I<Domain>ImplementationConfiguration contract, lives in its own table joined to the
// domain row, and is where every field the factory reads belongs. It is here only so a freshly
// generated pair compiles standalone. When you wire this to a real domain, implement that contract
// and add [ManagedConfiguration] and [GenerateMapper] the way EmailNotificationConfiguration does.
public sealed class ImplNameServiceNameConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets this implementation row's own identifier.</summary>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    /// <remarks>Set by the domain provider from the domain row; never persisted here.</remarks>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the domain provider from the domain row; never persisted here.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the domain provider from the domain row; never persisted here.</remarks>
    public string Implementation { get; set; } = string.Empty;
}
