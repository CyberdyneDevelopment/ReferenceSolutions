using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Tds;
using Fdw.Services.Hosts;
using Fdw.Services.Hosts.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Tds.Endpoints;
using Fdw.Tds.Endpoints.Hosting;
using FdwReferenceHosts.SqlServerEndpoint.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FdwReferenceHosts.SqlServerEndpoint.Registration;

/// <summary>
/// Gives a host a SQL Server endpoint: a TDS listener that presents the workspaces a caller may read as the
/// schemas of one SQL Server database, and the data sets attached to each as its tables.
/// </summary>
/// <remarks>
/// <para>
/// Referencing this package is what a host does to listen for SQL Server clients. Where it listens, the
/// server it presents itself as, the flow a SQL login runs and the certificate it encrypts with are the
/// host's SqlServerEndpoint row, read when the listener starts; a row that is missing or cannot be used
/// stops the host rather than leaving it running without the endpoint it was configured to have.
/// </para>
/// <para>
/// A SQL login is a password login: the flow the row names must run the PasswordCredential step and deliver
/// a session. The host must also register the workspace endpoints group, which registers the workspace read
/// service every table is read through.
/// </para>
/// </remarks>
[Implementation(typeof(HostTypes), "SqlServerEndpoint")]
public sealed class SqlServerEndpointHostType
    : HostTypeBase<SqlServerEndpointHostService, SqlServerEndpointHostConfiguration, ISqlServerEndpointHostFactory>
{
    // The implementation this option supplies to its domain -- the Implementation its domain row names.
    private const string HostImplementation = "Host";

    // The store the host features are read from: server settings, not platform configuration.
    private static string ConfigurationStore => ServerConnection.Name;

    /// <summary>Initializes a new instance of the <see cref="SqlServerEndpointHostType"/> class.</summary>
    public SqlServerEndpointHostType()
        : base(
            name: "SqlServerEndpoint",
            sectionName: "Host",
            displayName: "SQL Server endpoint",
            description: "A TDS listener presenting the caller's workspaces as a SQL Server database, read from the server configuration tier",
            defaultPathName: "sqe",
            defaultContainerName: "HostSqlServerEndpointConfiguration")
    {
        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(ISqlServerEndpointHostFactory), typeof(SqlServerEndpointHostFactory), ServiceLifetime.Singleton));

            builder.Services.AddSingleton<ISqlServerEndpointHostConfigurationProvider, SqlServerEndpointHostConfigurationProvider>(sp => new SqlServerEndpointHostConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<SqlServerEndpointHostConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                ConfigurationStore, PathName, Container));

            // FDW supplies the SqlServerEndpoint domain provider, not its registration: the host names the
            // implementation it dispatches to.
            builder.Services.TryAddSingleton(sp =>
            {
                var domain = new SqlServerEndpointConfigurationProvider(
                    sp.GetRequiredService<ILoggerFactory>().CreateLogger<SqlServerEndpointConfigurationProvider>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ConfigurationStore, "sqe", "SqlServerEndpoint");
                domain.Register(HostImplementation, sp.GetRequiredService<ISqlServerEndpointHostConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<ISqlServerEndpointConfigurationProvider>(sp => sp.GetRequiredService<SqlServerEndpointConfigurationProvider>());

            // Why a source rather than the options: the certificate is a secret, and nothing reads a
            // secret while the container is still being built. The listener reads it when it starts.
            builder.Services.TryAddSingleton<ITdsEndpointOptionsSource>(sp => new SqlServerEndpointOptionsSource(
                sp.GetRequiredService<ISqlServerEndpointConfigurationProvider>(),
                sp.GetRequiredService<ISecretManagerProvider>(),
                Name,
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<SqlServerEndpointOptionsSource>()));

            // Resolved in each request's scope, after the session's principal is set: everything it reads
            // is read as that caller.
            builder.Services.TryAddScoped<TdsEndpointBase>(sp => new WorkspaceTdsEndpoint(
                sp.GetRequiredService<IWorkspaceReadService>(),
                sp.GetRequiredService<IDataGatewayProvider>(),
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<WorkspaceTdsEndpoint>()));

            builder.Services.AddHostedService<TdsListener>();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
