using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Sample;

/// <summary>Validates inbound tokens issued by SAMPLE_MECHANISM.</summary>
/// <remarks>
/// One declared AuthenticationServices entry selects this mechanism by naming SAMPLE_MECHANISM in
/// its Implementation, and this option turns that entry into one scheme bound to one issuer. A
/// host may declare several entries, and each becomes its own binding — the selector then picks by
/// the token's issuer, so trusting a second issuer is another row rather than a code change.
///
/// Those entries are read through the configuration gateway, which is why the scheme is taken
/// during Initialize against the built container rather than added to the AuthenticationBuilder:
/// by the time the entry is readable, the builder is closed. That is what
/// <see cref="IAuthenticationSchemeProvider"/> is for here.
/// </remarks>
[ExcludeFromCodeCoverage]
[Implementation(typeof(AuthenticationServiceTypes), "SAMPLE_MECHANISM")]
public sealed class SampleAuthenticationType : AuthenticationServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SampleAuthenticationType"/> class.</summary>
    public SampleAuthenticationType()
        : base("SAMPLE_MECHANISM", "SAMPLE_MECHANISM", "Validates tokens issued by SAMPLE_MECHANISM.")
    {
    }

    /// <inheritdoc />
    public override string[] SupportedProtocols => new[] { "oidc" };

    /// <inheritdoc />
    public override string ProviderName => "SAMPLE_MECHANISM";

    /// <inheritdoc />
    public override IReadOnlyList<string> SupportedFlows => new[] { "client_credentials" };

    /// <inheritdoc />
    public override IReadOnlyList<string> SupportedTokenTypes => new[] { "Bearer" };

    /// <inheritdoc />
    public override int Priority => 100;

    /// <inheritdoc />
    public override bool SupportsMultiTenant => false;

    /// <inheritdoc />
    public override bool SupportsTokenCaching => true;

    /// <inheritdoc />
    public override IGenericResult<AuthenticationSchemeBinding> TakeScheme(
        IAuthenticationServiceImplementationConfiguration configuration,
        IAuthenticationSchemeProvider schemes,
        IServiceProvider services,
        ILoggerFactory? loggerFactory)
    {
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));
        if (schemes is null) throw new ArgumentNullException(nameof(schemes));

        var log = loggerFactory?.CreateLogger<SampleAuthenticationType>()
            ?? NullLogger<SampleAuthenticationType>.Instance;

        // NO FALLBACKS: an entry with no name cannot be told apart from another, and one with no
        // authority cannot validate anything — a scheme built on an assumed authority accepts
        // tokens nobody vouched for. Report the missing value and take no scheme; every route that
        // would have trusted this issuer then refuses, which is the honest outcome.
        if (configuration.Name is not { Length: > 0 } serviceName)
        {
            return GenericResult<AuthenticationSchemeBinding>.Failure(
                SampleAuthenticationLog.EntryMissingName(log));
        }

        if (configuration.Authority is not { Length: > 0 } authority)
        {
            return GenericResult<AuthenticationSchemeBinding>.Failure(
                SampleAuthenticationLog.AuthorityMissing(log, serviceName));
        }

        // Add the scheme this entry describes, naming the handler that validates its tokens:
        //   schemes.AddScheme(new AuthenticationScheme(
        //       SchemeNameFor(serviceName), displayName: null, handlerType: typeof(SampleAuthenticationHandler)));
        // then bind it to the issuer it accepts, so the selector can route a token by its iss.

        return GenericResult<AuthenticationSchemeBinding>.Success(
            new AuthenticationSchemeBinding(serviceName, authority, SchemeNameFor(serviceName)));
    }

    /// <summary>The scheme-name prefix this mechanism owns.</summary>
    /// <remarks>
    /// Qualified by mechanism so two entries of different mechanisms can share a name without one
    /// silently replacing the other's scheme.
    /// </remarks>
    public const string SchemePrefix = "SAMPLE_MECHANISM.";

    /// <summary>The scheme name this option takes for a given entry.</summary>
    /// <param name="serviceName">The declared entry's name.</param>
    public static string SchemeNameFor(string serviceName) => SchemePrefix + serviceName;
}
