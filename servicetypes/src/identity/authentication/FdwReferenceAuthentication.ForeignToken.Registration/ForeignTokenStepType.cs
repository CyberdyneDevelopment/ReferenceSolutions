using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Flow;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FdwReferenceAuthentication.ForeignToken.Registration;

/// <summary>
/// Accepts a token an external authority already issued, and turns it into a subject.
/// </summary>
/// <remarks>
/// <para>
/// The mechanism - verify a JWT against an issuer's published keys - is the same for Auth0, Entra or
/// Authentik, and only the issuer, audiences and algorithms differ. That is why the step itself lives
/// here rather than in the framework: what is NOT framework is the decision that THIS deployment
/// will exchange tokens from THIS authority on THESE terms, and the step's job is inseparable from
/// reading that decision. The claims a foreign token carries are the authority's assertions, not
/// authority here, so they arrive marked <see cref="ClaimSources.External"/> and change no decision
/// without an explicit mapping.
/// </para>
/// <para>
/// This is the platform side of RFC 8693 token exchange - a foreign token in, one of ours out - so a
/// flow using it should be reachable at the token endpoint's exchange grant rather than through a
/// bespoke endpoint.
/// </para>
/// <para>
/// A host that never references this package cannot name <c>ForeignToken</c> in a flow, which is the
/// intended outcome rather than an oversight.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[Implementation(typeof(AuthenticationStepTypes), "ForeignToken")]
public sealed class ForeignTokenStepType
    : AuthenticationStepTypeBase<IGenericService, IServiceFactory<IGenericService, IServiceConfiguration>>,
      IAuthenticationStep
{
    // Captured when the host is built: an option is created by its module initializer, which needs
    // a parameterless constructor, so what it needs arrives where a live container exists.
    private ForeignAuthorityReader? _authorities;
    private ISigningKeyProvider? _keys;
    private IForeignTokenAccessor? _presented;
    private ILogger _logger = NullLogger<ForeignTokenStepType>.Instance;

    // Set from the authority when the step runs. The runner reads AuthenticationMethods only after
    // Execute has returned, so this is populated by the time anything consults it.
    private IReadOnlyList<string> _asserted = [];

    /// <summary>Initializes a new instance of the <see cref="ForeignTokenStepType"/> class.</summary>
    public ForeignTokenStepType()
        : base("ForeignToken",
               "ForeignAuthorities",
               "Foreign Token",
               "Accepts a token another authority signed and carries its subject into the flow")
    {
        Registration((builder, loggerFactory) =>
        {
            // The authority rows THIS deployment declares. On the auth path beside the flow they
            // serve, so a reader looking for what a flow trusts finds it next to the flow.
            builder.Services.AddSingleton<IForeignAuthorityImplementationConfigurationProvider, ForeignAuthorityImplementationConfigurationProvider>(sp => new ForeignAuthorityImplementationConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<ForeignAuthorityImplementationConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                AuthenticationStepTypes.ConfigurationConnection, "auth", "ForeignAuthorityConfiguration"));

            builder.Services.TryAddSingleton<ForeignAuthorityConfigurationProvider>(sp =>
            {
                var domain = new ForeignAuthorityConfigurationProvider(
                    sp.GetRequiredService<ILogger<ForeignAuthorityConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    AuthenticationStepTypes.ConfigurationConnection, "auth", "ForeignAuthority");
                domain.Register("ForeignAuthority", sp.GetRequiredService<IForeignAuthorityImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IForeignAuthorityConfigurationProvider>(sp => sp.GetRequiredService<ForeignAuthorityConfigurationProvider>());

            // What the step reads its authority through. Not a provider -- it states what a usable
            // authority row is, and that judgement is this package's business.
            builder.Services.TryAddScoped<ForeignAuthorityReader>();

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Initialization((host, loggerFactory) =>
        {
            _authorities = host.Services.GetRequiredService<ForeignAuthorityReader>();
            _keys = host.Services.GetRequiredService<ISigningKeyProvider>();
            _presented = host.Services.GetRequiredService<IForeignTokenAccessor>();
            _logger = loggerFactory?.CreateLogger<ForeignTokenStepType>()
                ?? NullLogger<ForeignTokenStepType>.Instance;

            return GenericResult<IHost>.Success(host);
        });
    }

    /// <inheritdoc />
    public IReadOnlyList<IContextElement> Requires => [];

    /// <inheritdoc />
    public IReadOnlyList<IContextElement> Contributes => [ContextElements.Subject, ContextElements.Claims];

    /// <inheritdoc />
    /// <remarks>
    /// What the authority read during <see cref="Execute"/> permits, which is why this is read from
    /// a field rather than a provider: the runner consults it after the step has run, to keep only
    /// what the step both observed and was permitted to assert. Before the step runs it is empty,
    /// and a step declaring nothing proves nothing - so the failure direction is closed.
    /// </remarks>
    public IReadOnlyList<string> AuthenticationMethods => _asserted;

    /// <inheritdoc />
    public async Task<IGenericResult<StepOutcome>> Execute(
        AuthenticationContext context, ITenantProvider tenants, CancellationToken cancellationToken = default)
    {
        // An option whose Initialize never ran cannot read the authority it is supposed to
        // verify against, and accepting a foreign token without that is accepting anyone's.
        if (_authorities is null || _keys is null || _presented is null)
            return GenericResult<StepOutcome>.Failure(ForeignTokenLog.NotInitialized(_logger, Name));

        // Read before anything else, because every check below is stated in its terms and the
        // failure to read one is a different thing from a token that failed to satisfy it.
        var authority = await _authorities.Get(cancellationToken).ConfigureAwait(false);
        if (!authority.IsSuccess || authority.Value is null)
            return authority.ToNewResult<StepOutcome>();

        var configuration = authority.Value;
        _asserted = configuration.AssertableMethods;

        var token = _presented.Token;
        if (string.IsNullOrWhiteSpace(token))
            return GenericResult<StepOutcome>.Failure(
                ForeignTokenLog.NoTokenPresented(_logger, configuration.Issuer));

        var keys = await _keys.Current(configuration.JwksUri, cancellationToken).ConfigureAwait(false);
        if (keys.IsFailure)
            return keys.ToNewResult<StepOutcome>();

        var validated = await new JsonWebTokenHandler()
            .ValidateTokenAsync(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = configuration.Issuer,
                ValidateAudience = true,
                ValidAudiences = configuration.ValidAudiences,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = keys.Value,
                ValidAlgorithms = configuration.ValidAlgorithms,
                ClockSkew = configuration.ClockSkew,
            })
            .ConfigureAwait(false);

        if (!validated.IsValid)
        {
            return GenericResult<StepOutcome>.Failure(
                ForeignTokenLog.Rejected(_logger, configuration.Issuer,
                    validated.Exception?.GetType().Name ?? "unknown"));
        }

        var subjectId = validated.ClaimsIdentity.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(subjectId))
            return GenericResult<StepOutcome>.Failure(
                ForeignTokenLog.NoSubjectClaim(_logger, configuration.Issuer));

        ForeignTokenLog.Accepted(_logger, configuration.Issuer);

        return GenericResult<StepOutcome>.Success(new StepOutcome.Contributed(new ContextContribution
        {
            ObservedMethods = [.. validated.ClaimsIdentity.FindAll("amr").Select(c => c.Value)],

            Subject = new Subject
            {
                Issuer = configuration.Issuer,
                SubjectId = subjectId,
                AuthenticatedAt = DateTimeOffset.UtcNow,
            },
            Claims = [.. validated.ClaimsIdentity.Claims
                .Where(c => c.Type is not ("sub" or "aud" or "iss" or "exp" or "nbf" or "iat"))
                .Select(c => new Claim
                {
                    Type = c.Type,
                    Value = c.Value,
                    Source = ClaimSources.External,
                    Issuer = configuration.Issuer,
                })],
        }));
    }
}
