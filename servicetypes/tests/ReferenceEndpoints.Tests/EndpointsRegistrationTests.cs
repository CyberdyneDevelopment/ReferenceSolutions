using ReferenceEndpoints;
using System;
using System.Linq;
using FastEndpoints;
using Fdw.Web.RestEndpoints.Tests.Logging;
using Microsoft.AspNetCore.Builder;
using Shouldly;
using Xunit;

namespace ReferenceEndpoints.Tests;

// Why an alias rather than a plain using of the containing namespace: from inside
// Fdw.Web.RestEndpoints.Tests.EndpointTypeOptions the bare name `Endpoints` binds to the SIBLING
// NAMESPACE Fdw.Web.Endpoints, found by walking out to Fdw.Web, and the collection under test
// becomes unreachable. An alias declared in this namespace is resolved before that walk begins.
using Endpoints = global::Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints;

/// <summary>
/// A host that joined no endpoint group registers successfully and touches FastEndpoints in neither phase.
/// </summary>
/// <remarks>
/// This is the failure that took a Blazor skin down: the collection treated "no endpoints" as a
/// broken registration chain and failed the host, when a skin serving no REST endpoints is a
/// legitimate state. The fix is a guard, and a guard on a condition nothing reproduces is a guard
/// that quietly stops holding.
///
/// The two phases are asserted against the SAME host on purpose. Add and Use are a pair — skipping
/// AddFastEndpoints while still calling UseFastEndpoints is what produces "No service for type
/// 'FastEndpoint...'" at the first request, which builds and starts clean and only fails in
/// production. <see cref="UseFastEndpointsWithoutAddFastEndpointsIsWhatTheGuardAvoids"/> below pins
/// that consequence so this class fails loudly rather than vacuously if the pair is ever split.
/// </remarks>
[Trait("Priority", "P0")]
[Trait("Category", "CoreFramework")]
public sealed class EndpointsRegistrationTests
{
    // Why the premise is asserted rather than assumed: the guard under test only engages when no
    // group joined, and EndpointGroups is filled by [TypeOption] declarations that arrive with a
    // package reference. Every one of them lives in reference-servicetypes, so this assembly sees
    // none — but referencing such a package from this test project one day would turn every test
    // below green for the wrong reason. This makes that a failure instead.
    private static void RequireNoGroupsJoined() => Endpoints.Groups().Count.ShouldBe(
        0,
        "A package declaring a [TypeOption] of EndpointGroups has been referenced from this test " +
        "project. The no-groups guard is no longer the path under test.");

    /// <summary>No group joined, so nothing about FastEndpoints happens in either phase.</summary>
    /// <remarks>
    /// Why <c>force: true</c>: both phases latch on first run and then return Success unconditionally,
    /// so an unforced second call would assert against a latch rather than against the body.
    ///
    /// Why the service count is the assertion: on the non-empty path this body calls AddFastEndpoints,
    /// AddHttpContextAccessor and SwaggerDocument. Counting descriptors catches all three without
    /// naming FastEndpoints internals that are free to change.
    /// </remarks>
    [Fact]
    public void RegisterAndInitializeBothSkipFastEndpointsWhenNoGroupJoined()
    {
        RequireNoGroupsJoined();
        var recorder = new RecordingLogger();
        using var loggerFactory = new RecordingLoggerFactory(recorder);
        var builder = WebApplication.CreateBuilder();
        var before = builder.Services.Count;

        Endpoints.Register(builder, loggerFactory, force: true).IsSuccess.ShouldBeTrue();

        builder.Services.Count.ShouldBe(before);

        using var app = builder.Build();

        Endpoints.Initialize(app, loggerFactory, force: true).IsSuccess.ShouldBeTrue();
    }

    /// <summary>Both phases say in the log that they skipped, and say which phase they were.</summary>
    /// <remarks>
    /// The skip is invisible from the outside — a host that serves no endpoints looks identical to
    /// one whose registration silently did nothing. These two lines are the only difference, so the
    /// pair being present is part of the behaviour, not decoration around it.
    /// </remarks>
    [Fact]
    public void BothPhasesRecordTheSkipNamingThemselves()
    {
        RequireNoGroupsJoined();
        var recorder = new RecordingLogger();
        using var loggerFactory = new RecordingLoggerFactory(recorder);
        var builder = WebApplication.CreateBuilder();

        Endpoints.Register(builder, loggerFactory, force: true);
        using var app = builder.Build();
        Endpoints.Initialize(app, loggerFactory, force: true);

        recorder.Entries
            .Where(e => e.EventId.Id == 11023)
            .Select(e => e.Message)
            .ShouldBe(
            [
                "No endpoint groups joined; this host serves no REST endpoints, so FastEndpoints registration is skipped",
                "No endpoint groups joined; this host serves no REST endpoints, so FastEndpoints initialization is skipped",
            ]);
    }

    /// <summary>The group count is narrated even when it is zero.</summary>
    /// <remarks>
    /// Emitted before the guard, so "how many groups did this host see" is answerable without
    /// inferring it from the absence of later lines.
    /// </remarks>
    [Fact]
    public void RegisterNarratesAZeroGroupCount()
    {
        RequireNoGroupsJoined();
        var recorder = new RecordingLogger();
        using var loggerFactory = new RecordingLoggerFactory(recorder);

        Endpoints.Register(WebApplication.CreateBuilder(), loggerFactory, force: true);

        recorder.Entries
            .Where(e => e.EventId.Id == 11024)
            .Select(e => e.Message)
            .ShouldContain("Endpoint registration starting over 0 joined group(s)");
    }

    /// <summary>Using FastEndpoints without having added it is the failure the paired guard prevents.</summary>
    /// <remarks>
    /// This is the counterfactual that makes the Initialize half of the test above worth having: it
    /// shows that reaching <c>UseFastEndpoints</c> on a host where <c>AddFastEndpoints</c> was
    /// skipped is fatal, so Initialize returning Success is evidence the guard ran rather than
    /// evidence the call was harmless.
    ///
    /// The message is asserted, not just the type: "No service for type 'FastEndpoints.IServiceResolver'"
    /// is the exact production symptom the paired guards exist to prevent, and an
    /// InvalidOperationException from anywhere else in Build or Use would otherwise satisfy this.
    /// </remarks>
    [Fact]
    public void UseFastEndpointsWithoutAddFastEndpointsIsWhatTheGuardAvoids()
    {
        using var app = WebApplication.CreateBuilder().Build();

        Should.Throw<InvalidOperationException>(() => app.UseFastEndpoints())
            .Message.ShouldBe("No service for type 'FastEndpoints.IServiceResolver' has been registered.");
    }
}
