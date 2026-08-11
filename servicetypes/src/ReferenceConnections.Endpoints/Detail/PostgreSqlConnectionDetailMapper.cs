using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Connections.PostgreSql;
using Fdw.Services.Connections.PostgreSql.Authentication;

namespace ReferenceConnections.Endpoints;

/// <summary>Projects a PostgreSql typed body onto the detail DTO (mirrors CreatePostgreSqlConnectionEndpoint.MapToDetail).</summary>
[ExcludeFromCodeCoverage]
internal sealed class PostgreSqlConnectionDetailMapper : IConnectionDetailMapper
{
    /// <inheritdoc />
    public string ServiceOptionType => "PostgreSql";

    /// <inheritdoc />
    public void Map(ConnectionConfiguration parent, IConnectionConfiguration body, ConnectionDetailDto dto)
    {
        var c = (PostgreSqlConnectionConfiguration)body;
        dto.Server = c.Host;
        dto.Port = c.Port;
        dto.Database = c.Database;
        dto.AuthenticationType = c.AuthenticationType;
        dto.Authentication = PostgreSqlAuthenticationTypes.ByName(c.AuthenticationType)
            .MaskSecrets(new Dictionary<string, string?>(c.AdditionalProperties, StringComparer.OrdinalIgnoreCase), ConnectionDetailDto.MaskedSecretValue);
    }
}
