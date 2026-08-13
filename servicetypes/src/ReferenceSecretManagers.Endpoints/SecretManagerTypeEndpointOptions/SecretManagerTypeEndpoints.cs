using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceSecretManagers.Endpoints.SecretManagerTypeEndpointOptions;

/// <summary>The endpoints over the secret-manager-type resource.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(SecretManagerTypeEndpointBase), typeof(IEndpointTypeOption), typeof(SecretManagerTypeEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "SecretManagerTypeEndpoints")]
public partial class SecretManagerTypeEndpoints : EndpointTypeCollectionBase<SecretManagerTypeEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
