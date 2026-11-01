using Fdw.Services.Connections.Abstractions;
using FdwReferenceConnections.FileSystem.Registration;

namespace FdwReferenceConnections.FileSystem.Tests;

/// <summary>
/// Asserts that a connection kind with no session-context concept declares exactly that, and
/// therefore demands nothing of a host built only from such kinds.
/// </summary>
/// <remarks>
/// FileSystem is the canonical example, and not an arbitrary one: a FileSystem-only UI host is the
/// exact host shape that a prior registration-generator version broke with
/// <c>"No service for type 'IAuthenticationContextAccessor'"</c> by bracketing every domain's
/// <c>Initialize</c> in a system-elevation scope. Only the MsSql connection option registers that
/// accessor, so a host that never references it must never be made to require one. These assertions
/// hold the "declares nothing, needs nothing" end of that invariant; the generator end is held by
/// <c>PlatformServicesRegistrationGeneratorTests.NeverWrapsOrElevatesInitializeRegardlessOfAuthAbstractionsVisibility</c>.
/// </remarks>
public sealed class FileSystemSessionContextDeclarationTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void FileSystemConnectionTypeDeclaresNoSessionContextConcept()
    {
        // Act
        var declared = new FileSystemConnectionType().SessionContextTypes;

        // Assert: the base default, inherited because this kind never overrides it. A populated
        // collection with a named member — not an empty list, which would be indistinguishable from
        // a kind that simply forgot to declare its contexts.
        declared.ShouldNotBeEmpty();
        declared.Select(c => c.Name).ShouldBe(NoSessionContextTypes.All().Select(c => c.Name));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void NoSessionContextCollectionNeverCarriesTheReferenceSchemesMembers()
    {
        // Why this specific exclusion: the reference scheme's system elevation is its own explicit
        // key (SESSION_CONTEXT('System') = '1'). If the no-session-context position ever acquired
        // that member, "this kind carries no session context" and "this connection is fully
        // system-elevated" would become the same declaration.
        NoSessionContextTypes.All()
            .Select(c => c.Name)
            .ShouldNotContain("SystemContext");
    }
}
