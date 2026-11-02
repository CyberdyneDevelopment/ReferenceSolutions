using Fdw.Services.Hosts.Abstractions;
using FdwReferenceHosts.EmptyBody.Configuration;

namespace FdwReferenceHosts.EmptyBody;

/// <summary>Builds the CORS host service from its configuration.</summary>
public interface IEmptyBodyHostFactory : IHostFactory<EmptyBodyHostService, EmptyBodyHostConfiguration>
{
}
