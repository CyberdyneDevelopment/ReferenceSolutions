using Fdw.Services.Abstractions;

using Fdw.Services.Credentials.Abstractions;
namespace FdwReferenceCredentials.Sql.Registration;

/// <summary>Supplies the SqlCredentialService implementation's own configuration.</summary>
public interface ISqlCredentialServiceConfigurationProvider
    : IImplementationConfigurationProvider<ICredentialServiceImplementationConfiguration>
{
}
