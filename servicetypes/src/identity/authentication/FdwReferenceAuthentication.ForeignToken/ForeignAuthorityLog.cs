using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuthentication.ForeignToken;

/// <summary>
/// What reading a foreign authority reports.
/// </summary>
public static partial class ForeignAuthorityLog
{
    /// <summary>Logs an authority that was read and will be trusted.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The authority's name.</param>
    /// <param name="issuer">The issuer it names.</param>
    /// <param name="audiences">How many audiences it accepts.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62001, Level = LogLevel.Information,
        Message = "Foreign authority '{name}' read: tokens from '{issuer}' may be exchanged for {audiences} audience(s)")]
    public static partial IGenericMessage Read(ILogger logger, string name, string issuer, int audiences);

    /// <summary>Logs that no authority is declared.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62002, Level = LogLevel.Error,
        Message = "A flow names a ForeignToken step but no foreign authority is declared, so there is no issuer to trust and nothing a presented token could be checked against")]
    public static partial IGenericMessage NoneDeclared(ILogger logger);

    /// <summary>Logs more than one declared authority.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="names">The authorities declared.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62003, Level = LogLevel.Error,
        Message = "More than one foreign authority is declared ({names}) and a ForeignToken step cannot choose between them. Which authority a caller may be exchanged from is the decision this row records, so declare exactly one")]
    public static partial IGenericMessage Ambiguous(ILogger logger, string names);

    /// <summary>Logs an authority with no issuer.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The authority's name.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62004, Level = LogLevel.Error,
        Message = "Foreign authority '{name}' declares no Issuer. It must be the value the authority puts in a token's iss claim, matched literally")]
    public static partial IGenericMessage IssuerMissing(ILogger logger, string name);

    /// <summary>Logs a JwksUri that is not an absolute URL.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The authority's name.</param>
    /// <param name="value">What was declared.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62005, Level = LogLevel.Error,
        Message = "Foreign authority '{name}' declares JwksUri '{value}', which is not an absolute URL. It must be the full address the authority publishes its signing keys at, usually the authority's own issuer address plus /.well-known/jwks.json but not always")]
    public static partial IGenericMessage JwksUriUnusable(ILogger logger, string name, string value);

    /// <summary>Logs an authority with no audiences.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The authority's name.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62006, Level = LogLevel.Error,
        Message = "Foreign authority '{name}' declares no ValidAudiences. Without one, every token this authority mints for any client would be accepted for exchange")]
    public static partial IGenericMessage AudiencesMissing(ILogger logger, string name);

    /// <summary>Logs an authority with no algorithms.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The authority's name.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62007, Level = LogLevel.Error,
        Message = "Foreign authority '{name}' declares no ValidAlgorithms. The accepted algorithms are the host's decision, not the token's — a token that chooses its own can choose one its key trivially satisfies")]
    public static partial IGenericMessage AlgorithmsMissing(ILogger logger, string name);

    /// <summary>Logs an authority with no assertable methods.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The authority's name.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62008, Level = LogLevel.Error,
        Message = "Foreign authority '{name}' declares no AssertableMethods. These are the RFC 8176 methods this authority is trusted to have actually verified, so an empty list is a statement that it verified nothing")]
    public static partial IGenericMessage MethodsMissing(ILogger logger, string name);
}
