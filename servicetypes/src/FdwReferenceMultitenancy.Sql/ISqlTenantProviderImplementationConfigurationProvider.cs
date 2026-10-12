using Fdw.Services.Multitenancy;
using Fdw.Services.Abstractions;

namespace FdwReferenceMultitenancy.Sql;

/// <summary>Supplies the SqlTenantProvider implementation's own configuration to the domain that registers it.</summary>
public interface ISqlTenantProviderImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IMultitenancyImplementationConfiguration>
{
}
