using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Fdw.Services.Connections.Abstractions.Authentication;
using Fdw.Services.Connections.Results;

namespace FdwReferenceConnections.FileSystem.Registration;

/// <summary>
/// Connection type definition for FileSystem connections.
/// Registers <see cref="IFileSystemConnectionFactory"/> and the
/// <see cref="FileSystemConnectionConfiguration"/> typed-body provider. Connection configuration
/// lives in PlatformConfiguration, not appsettings.
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(ConnectionTypes), "FileSystem")]
public sealed class FileSystemConnectionType
    : ConnectionTypeBase<IGenericConnection, IFileSystemConnectionFactory, FileSystemConnectionConfiguration>
{

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemConnectionType"/> class.
    /// </summary>
    public FileSystemConnectionType() : base(
        name: "FileSystem",
        sectionName: "FileSystem",
        displayName: "File System",
        description: "Local or network file system connection",
        category: "Storage", defaultContainerName: "FileSystemConnectionConfiguration")
    {
        // Endpoint behavior: the flat request carried the root folder in BaseUrl.
        EndpointAlias("baseUrl", nameof(FileSystemConnectionConfiguration.Root));

        // Why Initialize and not Register: this wiring needs a LIVE container (it resolves the
        // domain provider and its typed-body providers), and Register runs while the container
        // is still being built. Initialize runs after Build() with a real IServiceProvider.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            // Why: Typed body providers are registered with the header provider (ConnectionConfigurationProvider)
            // via discriminator dispatch. FileSystemConnectionConfiguration no longer inherits
            // IConnectionImplementationConfiguration — it implements IConnectionImplementationConfiguration directly.
            services.GetRequiredService<IConnectionConfigurationProvider>().Register(Name, services.GetRequiredService<IFileSystemConnectionConfigurationProvider>());

            // The configuration provider above tells the domain how to READ this option's rows; this
            // hands the runtime connection provider what it BUILDS a connection with. Without it
            // IConnectionProvider's registry stays empty and every connection of this kind fails with
            // "no factory for service option" -- which is how the credential vault lost AuthDb.
            var implementationRegistered = services.GetRequiredService<IConnectionProvider>()
                .Register(Name, () => services.GetRequiredService<IFileSystemConnectionProvider>());
            if (!implementationRegistered.IsSuccess) return implementationRegistered.ToNewResult<IHost>();

    
            return GenericResult<IHost>.Success(host);
        });

        Configuration(builder =>
        {

    
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        // Why Append and not Registration: Registration REPLACES the phase body, and
        // ConnectionTypeBase's constructor has already prepended this option's factory registration
        // onto it. Replacing therefore silently discards the base's contribution — which is exactly
        // how every connection kind stopped being creatable while each option's own wiring kept
        // working and logging success. Appending composes onto what the base put there.
        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IFileSystemConnectionFactory), typeof(FileSystemConnectionFactory), ServiceLifetime.Singleton));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IFileSystemConnectionProvider), typeof(FileSystemConnectionProvider), ServiceLifetime.Singleton));
            // Why (FDW-403 slice 2 follow-up): mirror Http/MsSql — register a typed
            // FileSystemConnectionConfigurationProvider so ConnectionProvider can resolve
            // child config rows (conn.FileSystemConnection) by parent ConnectionId.
            builder.Services.AddSingleton<IFileSystemConnectionConfigurationProvider, FileSystemConnectionConfigurationProvider>(sp => new FileSystemConnectionConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<FileSystemConnectionConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                ConnectionTypes.ConfigurationConnection, PathName, Container));
            // Why: RegisterFactory (below) requires ConnectionConfigurationProvider (the shared header
            // provider for the whole Connections domain) to already be registered. TryAddSingleton makes
            // this idempotent — every connection-kind option calls it, harmlessly redundant after the first.
            // Why here (FDW-1575): the reference application keeps an organization's documents as files in the store the organization is
            // assigned, which in this application is a FileSystem connection. TryAdd, so an application that keeps its documents elsewhere
            // (or dates them differently) registers its own IRetainedDocuments and the retention policy, hold and job work unchanged.
            builder.Services.TryAddScoped<IRetainedDocuments, OrganizationStoreRetainedDocuments>();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

    /// <summary>
    /// FileSystem supports FileRead and FileWrite capabilities.
    /// </summary>
    // Why: ByName() returns the TypeCollection singleton — never instantiate capabilities inline.
    public override IReadOnlyList<ICommandCapabilityType> SupportedCommands =>
    [
        CommandCapabilityTypes.ByName("FileRead"),
        CommandCapabilityTypes.ByName("FileWrite"),
    ];

    /// <inheritdoc/>
    /// <remarks>A file-system connection reads and writes under its root with the host process's own file-system access -- it carries no credential of its own -- so every such connection acts as the host.</remarks>
    protected override IGenericResult<bool> DecideHostIdentity(FileSystemConnectionConfiguration configuration)
        => GenericResult<bool>.Success(true);

    /// <inheritdoc/>
    /// <remarks>A file-system connection opens files with the host process's own access; it carries no stored credential.</remarks>
    protected override IGenericResult<ConnectionCredentialSlots> DescribeCredentials(FileSystemConnectionConfiguration configuration)
        => GenericResult<ConnectionCredentialSlots>.Success(ConnectionCredentialSlots.None);

    /// <inheritdoc/>
    protected override IGenericResult BindCredentialReferences(
        FileSystemConnectionConfiguration configuration,
        string secretManagerName,
        IReadOnlyDictionary<string, string> secretKeyByField)
        => GenericResult.Failure(
            ConnectionsResultCodes.ByName("CredentialsUndetermined"),
            ResultDetails.Create("name", configuration.Name, "reason", "this kind of connection carries no stored credential"));
}
