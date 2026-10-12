using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Transformations;
using Microsoft.Extensions.Logging.Abstractions;
using FieldMappingTransformationConfiguration = Fdw.Data.DataSets.FieldMappingTransformationConfiguration;

namespace Fdw.Services.Data.Tests;

/// <summary>Builds the field-mapping transformation resolver the data gateway requires (FDW-877).</summary>
internal static class TestFieldTransformations
{
    /// <summary>
    /// A real resolver over a configuration gateway that answers every attachment query with no rows,
    /// for tests that attach no transformations.
    /// </summary>
    public static FieldMappingTransformationResolver NoAttachments()
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<FieldMappingTransformationConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<FieldMappingTransformationConfiguration>>.Success([]));
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(p => p.Get(It.IsAny<string>()))
            .Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));

        var transformations = new TransformationConfigurationProvider(
            NullLogger<TransformationConfigurationProvider>.Instance, gateways.Object,
            "PlatformConfiguration", "transform", "Transformation");
        return new FieldMappingTransformationResolver(gateways.Object, transformations);
    }
}
