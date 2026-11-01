using Fdw.Services.Hosts.Abstractions;
using FdwReferenceHosts.WebMcp.Configuration;

namespace FdwReferenceHosts.WebMcp;

/// <summary>Builds the WebMCP host service from its configuration.</summary>
public interface IWebMcpHostFactory : IHostFactory<WebMcpHostService, WebMcpHostConfiguration>
{
}
