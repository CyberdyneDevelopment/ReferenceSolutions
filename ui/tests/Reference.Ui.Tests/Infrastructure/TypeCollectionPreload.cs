using System.Runtime.CompilerServices;

namespace Reference.Ui.Tests.Infrastructure;

/// <summary>
/// Forces the <c>reference-ui</c> assembly's module initializer (the generated
/// <c>TypeOptionRegistration</c>) to run before ANY test touches a TypeCollection.
///
/// Why: TypeCollections freeze on first access. The reference-ui assembly contributes
/// TypeOptions (DataSetPreviewMode → PreviewModes, CalculatedMappingType → MappingTypes, …) via
/// a module initializer that runs on first access to the reference-ui module. If a test touches
/// one of those collections (e.g. constructs a FieldMappingDto whose default reads
/// MappingTypes.Direct, or renders DataPreview) before the reference-ui module loads, the
/// collection freezes and reference-ui's later registration throws, corrupting global static
/// state for the rest of the process. Running the module ctor here at TEST-assembly load makes
/// registration order deterministic regardless of which test collection xUnit schedules first.
/// </summary>
internal static class TypeCollectionPreload
{
    [ModuleInitializer]
    internal static void Preload()
    {
        // Touch a type that lives in the reference-ui assembly so the runtime runs that
        // assembly's module constructor (which performs all TypeOption registration) now.
        RuntimeHelpers.RunModuleConstructor(
            typeof(global::Reference.Ui.Components.App).Module.ModuleHandle);
    }
}
