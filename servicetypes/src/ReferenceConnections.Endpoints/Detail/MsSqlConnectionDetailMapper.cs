using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Connections.MsSql;
using Fdw.Services.Connections.MsSql.Authentication;

namespace ReferenceConnections.Endpoints;

/// <summary>Projects an MsSql typed body onto the detail DTO (mirrors CreateConnectionEndpoint.MapToDetail).</summary>
[ExcludeFromCodeCoverage]
internal sealed class MsSqlConnectionDetailMapper : IConnectionDetailMapper
{
    /// <inheritdoc />
    public string ServiceOptionType => "MsSql";

    /// <inheritdoc />
    public void Map(ConnectionConfiguration parent, IConnectionConfiguration body, ConnectionDetailDto dto)
    {
        // Why: hard cast — the dispatch guarantees this mapper only runs for an MsSql body; a mismatch
        // is a registration bug and should fail loud, not silently no-op.
        var c = (MsSqlConnectionConfiguration)body;
        dto.Server = c.Server;
        dto.Port = c.Port;
        dto.Database = c.Database;
        dto.AuthenticationType = c.AuthenticationType;
        dto.Authentication = MsSqlAuthenticationTypes.ByName(c.AuthenticationType)
            .MaskSecrets(new Dictionary<string, string?>(c.AdditionalProperties, StringComparer.OrdinalIgnoreCase), ConnectionDetailDto.MaskedSecretValue);
        dto.TrustServerCertificate = c.TrustServerCertificate;
        dto.Encrypt = c.Encrypt;
    }
}
