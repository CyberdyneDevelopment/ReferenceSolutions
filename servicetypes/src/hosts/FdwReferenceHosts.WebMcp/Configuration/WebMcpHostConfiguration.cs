using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Hosts.Abstractions;

namespace FdwReferenceHosts.WebMcp.Configuration;

/// <summary>
/// What a host's WebMCP surface serves, read from the server configuration tier: the header and
/// client key every tool call carries, and the address the host reaches its own routes on.
/// </summary>
/// <remarks>
/// The settings are the WebMcp domain's contract (IWebMcpImplementationConfiguration) because FDW's
/// WebMcp registrants read them through the WebMcp domain provider without knowing this record. The
/// contract is WebMcp's own, so no other host option sees them.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "WebMcp", ServiceType = "Host")]
public sealed partial class WebMcpHostConfiguration : IWebMcpImplementationConfiguration
{
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name this configuration is resolved by.</summary>
    // Why it maps with no column of its own: the name is the domain's, and there is one
    // name for a configured member. The domain provider joins the domain row to this one,
    // and the join's result set is what carries it in.
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <summary>Gets or sets the header the client key travels in.</summary>
    /// <remarks>
    /// Carried in configuration because both sides must agree on it: the generated script writes the
    /// header and the server reads it, and a host that changed one without the other would fail
    /// every tool call with no indication why.
    /// </remarks>
    public string ApiKeyHeader { get; set; } = string.Empty;

    /// <summary>Gets or sets the secret manager that holds the client key.</summary>
    /// <remarks>
    /// The key is written into a publicly cached JavaScript file, so it belongs to a low-privilege,
    /// read-only identity. It lives in the secret manager rather than in this row, so rotating it is a
    /// secret write and the value never sits in a configuration file. Named together with
    /// <see cref="SecretKeyName"/>, or neither: a host that names neither serves no client key.
    /// </remarks>
    public string? SecretManagerName { get; set; }

    /// <summary>Gets or sets the client key's name in <see cref="SecretManagerName"/>.</summary>
    public string? SecretKeyName { get; set; }

    /// <summary>Gets or sets the absolute address this host reaches its own routes on.</summary>
    /// <remarks>
    /// A tool call that arrives over MCP is sent to the tool's own route here, carrying the calling
    /// agent's Authorization header. It is configuration because the request's Host header is the
    /// caller's to set. Empty when the host does not serve MCP.
    /// </remarks>
    public string? SelfBaseAddress { get; set; }
}
