// Why this project exists — read before deleting it as unused.
//
// A [ServiceTypeOption] registers itself through a module initializer emitted by whichever
// compilation CONTAINS it. Move the option into a new package and every consumer that referenced
// the old package silently stops receiving the registration. Nothing fails: the build is clean,
// the restore is clean, and the collection is simply short one member at runtime.
//
// That has happened three times in this codebase — DataVault, Credentials, and the orphaned
// Workflows test projects — each time with a green build.
//
// This project is the gate. It is an executable, so the generator emits a module initializer into
// it, and it references the .Registration packages, so that initializer lists every option they
// contribute. Counting those lines before and after a change is the only pre-merge check that
// catches a dropped registration.
//
// To run it:
//
//   dotnet build tests/RegistrationGate/RegistrationGate.csproj -c Release \
//       -p:EmitCompilerGeneratedFiles=true
//   find tests/RegistrationGate/obj/Release/net10.0/generated -name '*ModuleInitializer*.g.cs' \
//       -exec cat {} + | grep -c RegisterMember
//
// Take that number before your change and after it. It must not fall. Sum across EVERY
// *ModuleInitializer*.g.cs — there are four generators, and reading one file reports a false pass.
//
// Add a ProjectReference here for each new .Registration package, so the gate keeps covering them.
//
// Why there is no entry point below: this project lives under tests/, so the xUnit SDK generates
// one. Adding a top-level statement here collides with it (CS7022). The module initializer is
// emitted by the generator regardless — it does not need a Main.

