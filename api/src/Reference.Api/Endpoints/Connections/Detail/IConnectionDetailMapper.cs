using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints;

namespace Reference.Api.Endpoints;

/// <summary>
/// Projects a connection's polymorphic typed body onto <see cref="ConnectionDetailDto"/> for one
/// <c>ServiceOptionType</c>. The type-agnostic <see cref="GetConnectionEndpoint"/> dispatches to the
/// mapper registered for the connection's discriminator — the sanctioned alternative to a type switch,
/// so a single GET-by-name endpoint renders every connection type. Each mapper mirrors its type's
/// create-endpoint <c>MapToDetail</c> so create and get render identically.
/// </summary>
internal interface IConnectionDetailMapper
{
    /// <summary>The connection ServiceOptionType this mapper handles (e.g. "MsSql", "Http").</summary>
    string ServiceOptionType { get; }

    /// <summary>Fills the type-specific DTO fields from the typed body (already known to be this type).</summary>
    void Map(ConnectionConfiguration parent, IConnectionConfiguration body, ConnectionDetailDto dto);
}
