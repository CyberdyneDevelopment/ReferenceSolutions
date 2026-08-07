using System.Runtime.CompilerServices;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using ReferenceAuthentication.OpenIddict.Storage.Models;
using ReferenceAuthentication.OpenIddict;
using Fdw.Services.Authentication.OpenIddict;
using Fdw.Services.Authentication;
using Fdw.Services;
using Fdw;

namespace ReferenceAuthentication.OpenIddict.Storage;

/// <summary>
/// Registers the OpenIddict DataGateway record POCO mappers with <see cref="PocoMapperCollection"/>
/// at assembly load time via a module initializer.
/// </summary>
/// <remarks>
/// Why a [ModuleInitializer] instead of a static constructor on OpenIddictStoreBase:
/// the [GenerateMapper]-produced mappers carry [TypeOption(..., RestrictToCurrentCompilation = true)],
/// but PocoMapperCollection lives in Fdw.Data.Abstractions (a referenced assembly), so the
/// Collections.SourceGenerators do NOT emit a module initializer for them. The previous static cctor on
/// OpenIddictStoreBase only ran on first store instantiation — which happens at the first /connect/token
/// request, AFTER PocoMapperCollection has already been frozen during startup. That threw
/// "PocoMapperCollection is already frozen". A module initializer runs at assembly load, before the
/// collection freezes, so the mappers are present when the store first resolves them.
/// </remarks>
internal static class OpenIddictPocoMapperRegistration
{
    // Why: CA2255 discourages [ModuleInitializer] in libraries, but registering these cross-assembly
    // POCO mappers at assembly load is exactly the sanctioned "advanced" use — the mappers MUST be in
    // PocoMapperCollection before it freezes at startup, which a static cctor (lazy on first store use)
    // cannot guarantee. This mirrors the source-generated module-initializer registration pattern.
#pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    [ModuleInitializer]
    internal static void Register()
    {
        PocoMapperCollection.RegisterMember(new OpenIddictScopeRecordPocoMapper());
        PocoMapperCollection.RegisterMember(new OpenIddictScopeResourceRecordPocoMapper());
        PocoMapperCollection.RegisterMember(new OpenIddictApplicationRecordPocoMapper());
        PocoMapperCollection.RegisterMember(new OpenIddictApplicationPermissionRecordPocoMapper());
        PocoMapperCollection.RegisterMember(new OpenIddictApplicationRedirectUriRecordPocoMapper());
        PocoMapperCollection.RegisterMember(new OpenIddictApplicationPostLogoutRedirectUriRecordPocoMapper());
        PocoMapperCollection.RegisterMember(new OpenIddictApplicationRequirementRecordPocoMapper());
        PocoMapperCollection.RegisterMember(new OpenIddictAuthorizationRecordPocoMapper());
        PocoMapperCollection.RegisterMember(new OpenIddictAuthorizationScopeRecordPocoMapper());
        PocoMapperCollection.RegisterMember(new ExternalIdentityRecordPocoMapper());
        PocoMapperCollection.RegisterMember(new RevokedAccessTokenRecordPocoMapper());
    }
#pragma warning restore CA2255
}
