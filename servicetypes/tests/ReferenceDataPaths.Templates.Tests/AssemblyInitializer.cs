using System.Runtime.CompilerServices;
using ReferenceDataPaths.Templates;

namespace ReferenceDataPaths.Templates.Tests;

/// <summary>
/// Loads the package under test before any test reads a TypeCollection.
/// </summary>
/// <remarks>
/// Why a test project needs this: members reach their collection through a generated module
/// initializer, which the CLR runs when the declaring assembly loads — and the runtime loads
/// assemblies lazily, on first use. A test that reads a collection before anything has touched this
/// package would observe it frozen and empty. Touching one type here forces the load, so the
/// registration is complete before the first assertion regardless of test order.
///
/// This is the test host's composition root, and is the sanctioned place for it. The equivalent in
/// an application is a signal that a member was declared in the wrong project: the fix there is to
/// move the member into a package, not to force the application to load.
/// </remarks>
internal static class AssemblyInitializer
{
    [ModuleInitializer]
    internal static void Initialize() => _ = typeof(MyProjectsTemplate);
}
