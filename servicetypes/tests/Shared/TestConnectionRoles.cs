using System.Runtime.CompilerServices;
using Fdw.Services.Data.Abstractions;

namespace FdwReference.Tests;

/// <summary>Declares the connection roles a test host would declare in its <c>configurationSchema.json</c>, once per test process.</summary>
/// <remarks>
/// Compiled into every test project that references <c>Fdw.Services.Data.Abstractions</c> (see
/// <c>tests/Directory.Build.props</c>). The holders have no default, so code under test that reads
/// <c>PlatformConnection.Name</c>, <c>ServerConnection.Name</c> or <c>AuthConnection.Name</c> needs a declaration the same way a
/// running host does; a module initializer states it when the assembly loads, before any test reads a holder.
/// </remarks>
internal static class TestConnectionRoles
{
    [ModuleInitializer]
    internal static void Declare()
    {
        PlatformConnection.Declare("PlatformConfiguration");
        ServerConnection.Declare("ServerConfiguration");
        AuthConnection.Declare("AuthDb");
    }
}
