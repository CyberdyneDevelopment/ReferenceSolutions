using Fdw.Services.Abstractions;

using Fdw.Services.Hosts.Abstractions;
namespace FdwReferenceHosts.SecurityHeaders;

/// <summary>Supplies the SecurityHeadersHost implementation's own configuration.</summary>
public interface ISecurityHeadersHostConfigurationProvider
    : IImplementationConfigurationProvider<ISecurityHeadersImplementationConfiguration>
{
}
