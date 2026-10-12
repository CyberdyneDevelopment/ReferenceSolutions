using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Abstractions;

namespace FdwReferenceAuthentication.ForeignToken;

/// <summary>Supplies the ForeignAuthority implementation's own configuration to the domain that registers it.</summary>
public interface IForeignAuthorityImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IForeignAuthorityImplementationConfiguration>
{
}
