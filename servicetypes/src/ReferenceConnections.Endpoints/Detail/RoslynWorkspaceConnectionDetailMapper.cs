using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Connections.RoslynWorkspace;

namespace ReferenceConnections.Endpoints;

/// <summary>Projects a RoslynWorkspace typed body onto the detail DTO (mirrors CreateRoslynWorkspaceConnectionEndpoint.MapToDetail).</summary>
[ExcludeFromCodeCoverage]
internal sealed class RoslynWorkspaceConnectionDetailMapper : IConnectionDetailMapper
{
    /// <inheritdoc />
    public string ServiceOptionType => "RoslynWorkspace";

    /// <inheritdoc />
    public void Map(ConnectionConfiguration parent, IConnectionConfiguration body, ConnectionDetailDto dto)
    {
        // Why: RoslynWorkspace has no Server/Database; the create endpoint surfaces SolutionPath via
        // BaseUrl and ModeName via Protocol, so GET mirrors that for identical create/get responses.
        var c = (RoslynWorkspaceConnectionConfiguration)body;
        dto.BaseUrl = c.SolutionPath;
        dto.Protocol = c.ModeName;
    }
}
