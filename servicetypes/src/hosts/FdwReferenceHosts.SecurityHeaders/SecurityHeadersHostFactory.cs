using Fdw.Services;
using Fdw.Configuration;
using Fdw.Results;
using FdwReferenceHosts.SecurityHeaders.Configuration;

namespace FdwReferenceHosts.SecurityHeaders;

/// <summary>Builds the CORS host service from its configuration record.</summary>
public sealed class SecurityHeadersHostFactory : ServiceFactoryBase<SecurityHeadersHostService, SecurityHeadersHostConfiguration>, ISecurityHeadersHostFactory
{

    /// <inheritdoc />
    public override IGenericResult<SecurityHeadersHostService> Create(SecurityHeadersHostConfiguration configuration)
        => Create((IGenericConfiguration)configuration);


}
