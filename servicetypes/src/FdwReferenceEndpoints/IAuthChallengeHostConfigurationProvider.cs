using Fdw.Services.Abstractions;

using Fdw.Services.Hosts.Abstractions;
namespace FdwReferenceEndpoints;

/// <summary>Supplies the AuthChallengeHost implementation's own configuration.</summary>
public interface IAuthChallengeHostConfigurationProvider
    : IImplementationConfigurationProvider<IAuthChallengeImplementationConfiguration>
{
}
