using System;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Protocols.Tds;
using Fdw.Protocols.Tds.Types;
using Fdw.Results;
using Fdw.Services.Hosts.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Fdw.Tds.Endpoints.Hosting;
using FdwReferenceHosts.SqlServerEndpoint.Configuration;
using FdwReferenceHosts.SqlServerEndpoint.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceHosts.SqlServerEndpoint;

/// <summary>
/// Turns the host's SqlServerEndpoint row, and the secrets it names, into the options the listener starts with.
/// </summary>
/// <remarks>
/// <para>
/// Every setting is checked before the listener starts, and any one that cannot be used fails the read with
/// the setting named: the listener does not start on half a configuration.
/// </para>
/// <para>
/// The TLS certificate is a base64 PKCS#12 file in one secret and its password in another, read from the
/// secret manager the row names, as an HTTP connection reads its client certificate. It must carry its
/// private key, since the server proves its name with it.
/// </para>
/// </remarks>
public sealed class SqlServerEndpointOptionsSource : ITdsEndpointOptionsSource
{
    private readonly ISqlServerEndpointConfigurationProvider _rows;
    private readonly ISecretManagerProvider _secretManagers;
    private readonly string _name;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="SqlServerEndpointOptionsSource"/> class.</summary>
    /// <param name="rows">Reads the SqlServerEndpoint domain.</param>
    /// <param name="secretManagers">Provides the secret manager the certificate is read from.</param>
    /// <param name="name">The name of the row this host listens with.</param>
    /// <param name="logger">The logger.</param>
    public SqlServerEndpointOptionsSource(
        ISqlServerEndpointConfigurationProvider rows,
        ISecretManagerProvider secretManagers,
        string name,
        ILogger<SqlServerEndpointOptionsSource>? logger)
    {
        _rows = rows ?? throw new ArgumentNullException(nameof(rows));
        _secretManagers = secretManagers ?? throw new ArgumentNullException(nameof(secretManagers));
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _logger = logger ?? NullLogger<SqlServerEndpointOptionsSource>.Instance;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<TdsEndpointOptions>> Read(CancellationToken cancellationToken = default)
    {
        var row = await _rows.Get(_name, cancellationToken).ConfigureAwait(false);
        if (row.IsFailure || row.Value is not SqlServerEndpointHostConfiguration configuration)
            return GenericResult<TdsEndpointOptions>.Failure(SqlServerEndpointHostLog.RowMissing(_logger, _name));

        var identity = Identity(configuration);
        if (identity.IsFailure)
            return identity.ToNewResult<TdsEndpointOptions>();

        var listenOn = ListenOn(configuration);
        if (listenOn.IsFailure)
            return listenOn.ToNewResult<TdsEndpointOptions>();

        var limits = Limits(configuration);
        if (limits.IsFailure)
            return limits.ToNewResult<TdsEndpointOptions>();

        var certificate = await Certificate(configuration, cancellationToken).ConfigureAwait(false);
        if (certificate.IsFailure)
            return certificate.ToNewResult<TdsEndpointOptions>();

        SqlServerEndpointHostLog.OptionsRead(_logger, listenOn.Value!.ToString(), configuration.ServerName, configuration.LoginFlowName);
        return GenericResult<TdsEndpointOptions>.Success(new TdsEndpointOptions(
            listenOn.Value, certificate.Value!, configuration.LoginFlowName, identity.Value!,
            configuration.MaximumMessageBytes, configuration.MaximumConnections, TimeSpan.FromSeconds(configuration.LoginTimeoutSeconds)));
    }

    private IGenericResult<TdsServerIdentity> Identity(SqlServerEndpointHostConfiguration configuration)
    {
        var named = FirstFailure(
            Required(nameof(configuration.ServerName), configuration.ServerName),
            Required(nameof(configuration.Edition), configuration.Edition),
            Required(nameof(configuration.Language), configuration.Language),
            Required(nameof(configuration.DatabaseName), configuration.DatabaseName),
            Positive(nameof(configuration.EngineEdition), configuration.EngineEdition));
        if (named.IsFailure)
            return named.ToNewResult<TdsServerIdentity>();

        if (!TdsServerVersion.TryParse(configuration.ProductVersion, out var version))
            return Invalid<TdsServerIdentity>(nameof(configuration.ProductVersion), $"'{configuration.ProductVersion}' is not a version such as 16.0.4135.4");

        if (!TdsCollation.TryGet(configuration.Collation, out var collation))
            return Invalid<TdsServerIdentity>(nameof(configuration.Collation), $"'{configuration.Collation}' is not a collation this endpoint encodes");

        return GenericResult<TdsServerIdentity>.Success(new TdsServerIdentity(
            configuration.ServerName, version, configuration.Edition, configuration.EngineEdition, collation, configuration.Language, configuration.DatabaseName));
    }

    private IGenericResult<IPEndPoint> ListenOn(SqlServerEndpointHostConfiguration configuration)
    {
        if (!IPAddress.TryParse(configuration.ListenAddress, out var address))
            return Invalid<IPEndPoint>(nameof(configuration.ListenAddress), $"'{configuration.ListenAddress}' is not an IP address");

        return configuration.Port is >= 1 and <= IPEndPoint.MaxPort
            ? GenericResult<IPEndPoint>.Success(new IPEndPoint(address, configuration.Port))
            : Invalid<IPEndPoint>(nameof(configuration.Port), $"{configuration.Port} is not a TCP port");
    }

    private IGenericResult Limits(SqlServerEndpointHostConfiguration configuration)
        => FirstFailure(
            Required(nameof(configuration.LoginFlowName), configuration.LoginFlowName),
            AtLeastOnePacket(nameof(configuration.MaximumMessageBytes), configuration.MaximumMessageBytes),
            Positive(nameof(configuration.MaximumConnections), configuration.MaximumConnections),
            Positive(nameof(configuration.LoginTimeoutSeconds), configuration.LoginTimeoutSeconds));

    private async Task<IGenericResult<X509Certificate2>> Certificate(SqlServerEndpointHostConfiguration configuration, CancellationToken cancellationToken)
    {
        var named = FirstFailure(
            Required(nameof(configuration.SecretManagerName), configuration.SecretManagerName),
            Required(nameof(configuration.CertificateSecretName), configuration.CertificateSecretName),
            Required(nameof(configuration.CertificatePasswordSecretName), configuration.CertificatePasswordSecretName));
        if (named.IsFailure)
            return named.ToNewResult<X509Certificate2>();

        var manager = await _secretManagers.Get(configuration.SecretManagerName, cancellationToken).ConfigureAwait(false);
        if (manager.IsFailure || manager.Value is null)
            return Invalid<X509Certificate2>(nameof(configuration.SecretManagerName), $"secret manager '{configuration.SecretManagerName}' cannot be read");

        var file = await Secret(manager.Value, configuration.CertificateSecretName, configuration.SecretManagerName, cancellationToken).ConfigureAwait(false);
        if (file.IsFailure)
            return file.ToNewResult<X509Certificate2>();

        var password = await Secret(manager.Value, configuration.CertificatePasswordSecretName, configuration.SecretManagerName, cancellationToken).ConfigureAwait(false);
        return password.IsFailure
            ? password.ToNewResult<X509Certificate2>()
            : Load(configuration.CertificateSecretName, file.Value!, password.Value!);
    }

    private async Task<IGenericResult<string>> Secret(ISecretManager manager, string secretName, string secretManagerName, CancellationToken cancellationToken)
    {
        var read = await manager.Execute(GetSecretManagerCommand.Latest(null, secretName), cancellationToken).ConfigureAwait(false);
        return read.IsSuccess && read.Value is not null
            ? GenericResult<string>.Success(read.Value.GetStringValue())
            : GenericResult<string>.Failure(SqlServerEndpointHostLog.SecretUnreadable(_logger, secretName, secretManagerName));
    }

    private IGenericResult<X509Certificate2> Load(string secretName, string file, string password)
    {
        X509Certificate2 certificate;
        try
        {
            certificate = X509CertificateLoader.LoadPkcs12(Convert.FromBase64String(file), password);
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            return Invalid<X509Certificate2>(nameof(SqlServerEndpointHostConfiguration.CertificateSecretName),
                $"secret '{secretName}' is not a base64 PKCS#12 file its password opens");
        }

        if (certificate.HasPrivateKey)
            return GenericResult<X509Certificate2>.Success(certificate);

        certificate.Dispose();
        return Invalid<X509Certificate2>(nameof(SqlServerEndpointHostConfiguration.CertificateSecretName),
            $"the certificate in secret '{secretName}' carries no private key, so the server cannot prove its name with it");
    }

    // Every check runs, so every setting that cannot be used is logged at once; the first is the answer.
    private static IGenericResult FirstFailure(params IGenericResult[] checks)
    {
        foreach (var check in checks)
        {
            if (check.IsFailure)
                return check;
        }

        return GenericResult.Success();
    }

    private IGenericResult Required(string setting, string value)
        => string.IsNullOrWhiteSpace(value) ? Invalid(setting, "it is empty") : GenericResult.Success();

    private IGenericResult Positive(string setting, int value)
        => value > 0 ? GenericResult.Success() : Invalid(setting, $"{value} is not a positive number");

    // A message limit under one largest packet would refuse a client's first full packet, so every connection would
    // fail in its handshake while the listener looked healthy.
    private IGenericResult AtLeastOnePacket(string setting, int value)
        => value >= TdsMessageReader.SmallestLimit
            ? GenericResult.Success()
            : Invalid(setting, $"{value} is smaller than one {TdsMessageReader.SmallestLimit}-byte TDS packet");

    private IGenericResult Invalid(string setting, string reason)
        => GenericResult.Failure(SqlServerEndpointHostLog.SettingInvalid(_logger, setting, reason));

    private IGenericResult<T> Invalid<T>(string setting, string reason)
        => GenericResult<T>.Failure(SqlServerEndpointHostLog.SettingInvalid(_logger, setting, reason));
}
