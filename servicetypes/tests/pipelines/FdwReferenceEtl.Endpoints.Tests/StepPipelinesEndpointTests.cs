using System.Net;
using Fdw.Services.Etl.Projects.Abstractions.TypeCollections;

namespace FdwReferenceEtl.Endpoints.Tests;

/// <summary>
/// FDW-2094: <c>POST /nodes</c> gives a step its pipelines and the prerequisites between them, by pipeline name. A name that is
/// not a pipeline, a name sent twice, pipelines sent to a node that cannot hold them, a prerequisite outside the step and a
/// prerequisite loop are each refused with a client error and nothing is stored.
/// </summary>
public sealed class StepPipelinesEndpointTests : NodeEndpointTestBase
{
    private static object Step(object[]? pipelines = null, object[]? prerequisites = null, int? nodeTypeId = null) => new
    {
        name = "Load sales",
        nodeTypeId = nodeTypeId ?? OrchestrationNodeTypes.Step.Id,
        parentId = Guid.NewGuid(),
        pipelines,
        prerequisites
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStepIsStoredWithItsPipelinesAndPrerequisitesResolvedToPipelines()
    {
        var response = await Post(Step(
            [new { pipeline = "A", ordinal = 0 }, new { pipeline = "B", ordinal = 1 }, new { pipeline = "C", ordinal = 2 }],
            [new { pipeline = "C", needs = "A" }, new { pipeline = "C", needs = "B" }]));

        response.StatusCode.ShouldBe(HttpStatusCode.Created, await Body(response));
        VerifyStored(Times.Once());
        var saved = Saved.ShouldNotBeNull();
        saved.PipelineMemberships.Select(m => m.PipelineId)
            .ShouldBe(new[] { KnownPipelines["A"], KnownPipelines["B"], KnownPipelines["C"] });
        saved.PipelinePrerequisites.Select(p => (p.PipelineId, p.PrerequisitePipelineId))
            .ShouldBe(new[] { (KnownPipelines["C"], KnownPipelines["A"]), (KnownPipelines["C"], KnownPipelines["B"]) });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task APipelineNameThatIsNotAPipelineIsRefusedAndNothingIsStored()
    {
        var response = await Post(Step([new { pipeline = "NoSuchPipeline", ordinal = 0 }]));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, await Body(response));
        (await Detail(response)).ShouldContain("Pipeline 'NoSuchPipeline' does not exist");
        VerifyStored(Times.Never());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task APipelineNamedTwiceIsRefusedAndNothingIsStored()
    {
        var response = await Post(Step([new { pipeline = "A", ordinal = 0 }, new { pipeline = "A", ordinal = 1 }]));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, await Body(response));
        (await Detail(response)).ShouldContain("Pipeline 'A' is named more than once");
        VerifyStored(Times.Never());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ANodeThatCannotHoldPipelinesIsRefusedWhenSentPipelines()
    {
        var response = await Post(Step([new { pipeline = "A", ordinal = 0 }], nodeTypeId: OrchestrationNodeTypes.Stage.Id));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, await Body(response));
        (await Detail(response)).ShouldContain("only a Step can");
        VerifyStored(Times.Never());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task APrerequisiteOnAPipelineTheStepDoesNotHoldIsRefusedAtSaveAndNothingIsStored()
    {
        var response = await Post(Step(
            [new { pipeline = "A", ordinal = 0 }, new { pipeline = "B", ordinal = 1 }],
            [new { pipeline = "B", needs = "C" }]));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, await Body(response));
        (await Detail(response)).ShouldContain("does not hold");
        VerifyStored(Times.Never());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task PrerequisitesThatLoopAreRefusedAtSaveAndNothingIsStored()
    {
        var response = await Post(Step(
            [new { pipeline = "A", ordinal = 0 }, new { pipeline = "B", ordinal = 1 }],
            [new { pipeline = "A", needs = "B" }, new { pipeline = "B", needs = "A" }]));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, await Body(response));
        (await Detail(response)).ShouldContain("cycle");
        VerifyStored(Times.Never());
    }
}
