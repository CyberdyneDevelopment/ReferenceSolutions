using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceSecretManagers.Endpoints.SecretManagerEndpointOptions;

/// <summary>The endpoints over the secret-manager resource.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(SecretManagerEndpointBase), typeof(IEndpointTypeOption), typeof(SecretManagerEndpoints))]
public partial class SecretManagerEndpoints : EndpointTypeCollectionBase<SecretManagerEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
