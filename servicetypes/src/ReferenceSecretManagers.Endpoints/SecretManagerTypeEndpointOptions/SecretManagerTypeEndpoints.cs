using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceSecretManagers.Endpoints.SecretManagerTypeEndpointOptions;

/// <summary>The endpoints over the secret-manager-type resource.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "SecretManagerTypeEndpoints")]
[TypeCollection(typeof(SecretManagerTypeEndpointBase), typeof(IEndpointTypeOption), typeof(SecretManagerTypeEndpoints))]
public partial class SecretManagerTypeEndpoints : EndpointTypeCollectionBase<SecretManagerTypeEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
