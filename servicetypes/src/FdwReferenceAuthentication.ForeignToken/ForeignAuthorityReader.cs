using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Steps;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceAuthentication.ForeignToken;

/// <summary>
/// Builds what a <c>ForeignToken</c> step checks a token against, from the authority row.
/// </summary>
/// <remarks>
/// The step reads its authority through <see cref="IForeignAuthorityConfigurationProvider"/>, and
/// this is the implementation of it: it reads the single declared authority row and converts the
/// delimited columns into the lists the step wants. Splitting happens here, at the read, so the
/// delimited column keeps one column's shape rather than becoming a convention every consumer of
/// the row has to know.
/// <para>
/// This is why the vendor's own values live in this package and the step lives in the framework:
/// verifying a JWT against an issuer's published keys is identical for Auth0, Entra and Authentik,
/// and only these rows differ.
/// </para>
/// </remarks>
/// <remarks>Not a provider: it reads through the ForeignAuthority domain provider and states what a
/// usable authority row is. The validation is why it exists, so it does not belong on a provider.</remarks>
public sealed class ForeignAuthorityReader
{
    private readonly IForeignAuthorityConfigurationProvider _authorities;

    private readonly ILogger _log;

    /// <summary>Initializes a new instance of the <see cref="ForeignAuthorityReader"/> class.</summary>
    /// <param name="authorities">Reads the declared foreign authorities.</param>
    /// <param name="logger">The logger.</param>
    public ForeignAuthorityReader(
        IForeignAuthorityConfigurationProvider authorities,
        ILogger<ForeignAuthorityReader>? logger = null)
    {
        _authorities = authorities ?? throw new ArgumentNullException(nameof(authorities));
        _log = logger ?? NullLogger<ForeignAuthorityReader>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IForeignAuthorityConfiguration>> Get(
        CancellationToken cancellationToken = default)
    {
        var rows = await _authorities.Get(cancellationToken).ConfigureAwait(false);
        if (!rows.IsSuccess || rows.Value is null)
            return rows.ToNewResult<IForeignAuthorityConfiguration>();

        var live = rows.Value.Where(r => r.IsCurrent && !r.IsDeleted).ToList();

        if (live.Count == 0)
            return GenericResult<IForeignAuthorityConfiguration>.Failure(
                ForeignAuthorityLog.NoneDeclared(_log));

        // More than one is refused rather than guessed at. Which authority a caller may be exchanged
        // from is the decision this row exists to record, and picking by order would make it depend
        // on read order instead.
        if (live.Count > 1)
            return GenericResult<IForeignAuthorityConfiguration>.Failure(
                ForeignAuthorityLog.Ambiguous(_log, string.Join(", ", live.Select(r => r.Name))));

        var row = live[0];

        if (string.IsNullOrWhiteSpace(row.Issuer))
            return GenericResult<IForeignAuthorityConfiguration>.Failure(
                ForeignAuthorityLog.IssuerMissing(_log, row.Name));

        if (!Uri.TryCreate(row.JwksUri, UriKind.Absolute, out var jwks))
            return GenericResult<IForeignAuthorityConfiguration>.Failure(
                ForeignAuthorityLog.JwksUriUnusable(_log, row.Name, row.JwksUri));

        var audiences = Split(row.ValidAudiences);
        if (audiences.Length == 0)
            return GenericResult<IForeignAuthorityConfiguration>.Failure(
                ForeignAuthorityLog.AudiencesMissing(_log, row.Name));

        var algorithms = Split(row.ValidAlgorithms);
        if (algorithms.Length == 0)
            return GenericResult<IForeignAuthorityConfiguration>.Failure(
                ForeignAuthorityLog.AlgorithmsMissing(_log, row.Name));

        var methods = Split(row.AssertableMethods);
        if (methods.Length == 0)
            return GenericResult<IForeignAuthorityConfiguration>.Failure(
                ForeignAuthorityLog.MethodsMissing(_log, row.Name));

        ForeignAuthorityLog.Read(_log, row.Name, row.Issuer, audiences.Length);

        return GenericResult<IForeignAuthorityConfiguration>.Success(new ForeignTokenStepConfiguration
        {
            Issuer = row.Issuer,
            JwksUri = jwks,
            ValidAudiences = audiences,
            ValidAlgorithms = algorithms,
            AssertableMethods = methods,
            ClockSkew = TimeSpan.FromSeconds(row.ClockSkewSeconds),
        });
    }

    private static string[] Split(string value)
        => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
