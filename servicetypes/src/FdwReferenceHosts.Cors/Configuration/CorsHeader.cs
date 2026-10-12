using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace FdwReferenceHosts.Cors.Configuration;

/// <summary>A header the CORS policy names, either permitted on the request or exposed on the response.</summary>
/// <remarks>
/// One record for both directions, told apart by <see cref="IsExposed"/>. They were two types over two
/// identically-shaped containers, which left the two indistinguishable in the schema -- same fields, same
/// foreign key, same owner -- so nothing but the C# type name said which container held which, and the
/// configuration could not answer it. A header may legitimately appear twice, once in each direction:
/// <c>X-Correlation-Id</c> is both accepted on the request and exposed on the response.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Host", ServiceType = "CorsHeader")]
public sealed partial class CorsHeader : IGenericConfiguration
{
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }


    /// <summary>Gets or sets the header name.</summary>
    public string HeaderName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this header is EXPOSED on the response
    /// (<see langword="true"/>) or PERMITTED on the request (<see langword="false"/>).
    /// </summary>
    /// <remarks>
    /// No database default and no defaulted value here: which direction a header applies to is the whole
    /// content of the record, and a row that does not say is not a row with a sensible reading -- it is a
    /// row that would silently be treated as a request header and quietly drop a response header from the
    /// policy. The writer states it.
    /// </remarks>
    public bool IsExposed { get; set; }


}
