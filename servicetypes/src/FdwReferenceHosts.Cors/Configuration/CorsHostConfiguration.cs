using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Hosts.Abstractions;

namespace FdwReferenceHosts.Cors.Configuration;

/// <summary>
/// The CORS policy this host applies, read from the server configuration tier.
/// </summary>
/// <remarks>
/// Every setting reference-api previously bound with
/// the CORS policy lives here. Its settings are declared
/// on this record rather than on ICorsImplementationConfiguration so that adding an option never
/// widens a shared interface, and so no other option can see settings that are not its business.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Cors", ServiceType = "Host")]
public sealed partial class CorsHostConfiguration : ICorsImplementationConfiguration
{
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name this configuration is resolved by.</summary>
    // Why it maps with no column of its own: the name is the domain's, and there is one
    // name for a configured member. The domain provider joins the domain row to this one,
    // and the join's result set is what carries it in.
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the domain record this implementation belongs to.</summary>
    /// <summary>Gets or sets the domain record's durable id.</summary>
    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;


    /// <summary>Gets or sets a value indicating whether the CORS policy is applied at all.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets the origins the policy permits.</summary>
    public IList<CorsOrigin> Origins { get; set; } = new List<CorsOrigin>();

    /// <summary>Gets the methods the policy permits.</summary>
    public IList<CorsMethod> Methods { get; set; } = new List<CorsMethod>();

    /// <summary>Gets the headers the policy names, in both directions.</summary>
    /// <remarks>
    /// Request-permitted and response-exposed headers are one collection told apart by
    /// <see cref="CorsHeader.IsExposed"/>, because as two collections over two identically-shaped
    /// containers neither the schema nor the mapper could say which container belonged to which property.
    /// </remarks>
    public IList<CorsHeader> Headers { get; set; } = new List<CorsHeader>();

    /// <summary>Gets or sets a value indicating whether the policy permits credentials.</summary>
    public bool AllowCredentials { get; set; }

    /// <summary>Gets or sets how long a preflight response may be cached, in seconds.</summary>
    public int PreflightMaxAgeSeconds { get; set; }
}
