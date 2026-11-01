using System.Net;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Abstractions.TypeCollections;

namespace FdwReferenceEtl.Endpoints.Tests;

/// <summary>
/// FDW-2093: <c>POST /nodes</c> saves through the validating writer, so a node the validator refuses is answered with a
/// client error and nothing is stored (it used to call the provider directly and answer 500 for every failure).
/// </summary>
public sealed class CreateOrchestrationNodeEndpointTests : NodeEndpointTestBase
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStageWithNoParentIsRefusedWithAClientErrorAndNothingIsStored()
    {
        var response = await Post(new { name = "Load", nodeTypeId = OrchestrationNodeTypes.Stage.Id });

        ((int)response.StatusCode).ShouldBeInRange(400, 499, await Body(response));
        VerifyStored(Times.Never());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStageWithAParentIsStoredOnce()
    {
        var response = await Post(new { name = "Load", nodeTypeId = OrchestrationNodeTypes.Stage.Id, parentId = Guid.NewGuid() });

        response.StatusCode.ShouldBe(HttpStatusCode.Created, await Body(response));
        VerifyStored(Times.Once());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AStorageFailureIsAnsweredAsADependencyFailureNotAClientError()
    {
        Provider
            .Setup(p => p.Save(It.IsAny<OrchestrationNodeImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("the database is down"));

        var response = await Post(new { name = "Load", nodeTypeId = OrchestrationNodeTypes.Stage.Id, parentId = Guid.NewGuid() });

        // The writer reports a failed store as a Dependency result (category 7), which the shared mapper answers with 502.
        response.StatusCode.ShouldBe(HttpStatusCode.BadGateway, await Body(response));
    }
}
