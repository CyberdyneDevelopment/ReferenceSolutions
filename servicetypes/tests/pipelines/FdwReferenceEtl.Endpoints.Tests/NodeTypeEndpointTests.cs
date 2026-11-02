using System.Net;
using Fdw.Services.Etl.Projects.Abstractions.TypeCollections;

namespace FdwReferenceEtl.Endpoints.Tests;

/// <summary>
/// FDW-2101: <c>POST /nodes</c> says what the node is by name (Project, Stage, Step), resolved through the node type collection.
/// A request that says neither a name nor a number, says two that disagree, or names a type that does not exist is refused
/// with a client error and nothing is stored.
/// </summary>
public sealed class NodeTypeEndpointTests : NodeEndpointTestBase
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ANodeSentByTypeNameIsStoredWithThatType()
    {
        var response = await Post(new { name = "Load", nodeType = "Step", parentId = Guid.NewGuid() });

        response.StatusCode.ShouldBe(HttpStatusCode.Created, await Body(response));
        Saved.ShouldNotBeNull().NodeTypeId.ShouldBe(OrchestrationNodeTypes.Step.Id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ANodeSentByTypeIdIsStillStoredWithThatType()
    {
        var response = await Post(new { name = "Load", nodeTypeId = OrchestrationNodeTypes.Stage.Id, parentId = Guid.NewGuid() });

        response.StatusCode.ShouldBe(HttpStatusCode.Created, await Body(response));
        Saved.ShouldNotBeNull().NodeTypeId.ShouldBe(OrchestrationNodeTypes.Stage.Id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ANameAndANumberThatAgreeAreAccepted()
    {
        var response = await Post(new
        {
            name = "Load",
            nodeType = "Stage",
            nodeTypeId = OrchestrationNodeTypes.Stage.Id,
            parentId = Guid.NewGuid()
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created, await Body(response));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ANameAndANumberThatDisagreeAreRefusedAndNothingIsStored()
    {
        var response = await Post(new
        {
            name = "Load",
            nodeType = "Step",
            nodeTypeId = OrchestrationNodeTypes.Stage.Id,
            parentId = Guid.NewGuid()
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, await Body(response));
        VerifyStored(Times.Never());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ATypeNameThatIsNotANodeTypeIsRefusedAndNothingIsStored()
    {
        var response = await Post(new { name = "Load", nodeType = "Phase", parentId = Guid.NewGuid() });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, await Body(response));
        (await Detail(response)).ShouldContain("nodeType 'Phase' does not exist; send one of");
        VerifyStored(Times.Never());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ANodeThatSaysNeitherANameNorANumberIsRefusedAndNothingIsStored()
    {
        var response = await Post(new { name = "Load", parentId = Guid.NewGuid() });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, await Body(response));
        (await Detail(response)).ShouldContain("Say what the node is");
        VerifyStored(Times.Never());
    }
}
