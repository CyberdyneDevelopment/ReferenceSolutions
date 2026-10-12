using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using FdwReferenceEndpoints.ApiReference;

namespace FdwReferenceEndpoints.ApiReferenceEndpointOptions;

/// <summary>The whole-surface API reference document — GET /openapi/reference.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ApiReferenceEndpoints), "ServiceReferenceDocument")]
public class ServiceReferenceDocumentOption : ApiReferenceEndpointBase<ServiceReferenceDocumentEndpoint>
{
}
