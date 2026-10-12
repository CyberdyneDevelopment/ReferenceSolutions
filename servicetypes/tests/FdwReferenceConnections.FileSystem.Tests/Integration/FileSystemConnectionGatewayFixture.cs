using FdwReferenceConnections.FileSystem.Registration;
using FdwReferenceSecretManagers.EnvironmentVariable.Registration;
using System.Text.Json.Nodes;
using Fdw.ServiceTypes;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Configuration;
using Fdw.Services.Connections.Abstractions;

namespace FdwReferenceConnections.FileSystem.Tests.Integration;

/// <summary>
/// Builds ONE shared <see cref="ServiceProvider"/> for every test in
/// <see cref="FileSystemConnectionGatewayTests"/>. <see cref="SecretManagerTypes.Register"/> tracks
/// per-option registration in process-wide static state (<c>_registeredOptionNames</c>) — building
/// a fresh <see cref="ServiceCollection"/> per test would make every call after the first a silent
/// no-op against that new (empty) collection, leaving <see cref="SecretManagerConfigurationProvider"/>
/// unregistered. One shared, read-only-after-construction provider is the correct usage here, exactly
/// as a real host builds its container once.
/// </summary>
/// <remarks>
/// The store is rooted at an ISOLATED temp copy of the committed <c>config-data</c> seed (with a schema
/// clone whose <c>Root</c> repoints there), so the read tests still see the seed AND the end-to-end
/// write test (<see cref="SecretManagerConfigurationProvider.Save"/>) can persist a new record without
/// mutating the committed fixtures. The write test uses a DISTINCT record name so it can never interfere
/// with the seeded <c>EnvSecrets</c> the read tests assert on.
/// </remarks>
public sealed class FileSystemConnectionGatewayFixture : IDisposable
{
    public IServiceProvider Provider { get; }

    // Why the host is held: it owns the container now, so disposing it is what disposes the provider.
    private readonly IHost _host;

    public string Root { get; }

    public FileSystemConnectionGatewayFixture()
    {
        Root = Directory.CreateTempSubdirectory("fdw-fs-gateway-").FullName;
        CopySeed(Path.Combine(AppContext.BaseDirectory, "config-data"), Root);
        var schemaPath = WriteSchemaPointingAt(Root);

        var builder = Host.CreateApplicationBuilder();

        var services = builder.Services;
        services.AddLogging();
        // Why registered directly: ConfigurationGatewayTypes builds gateways from the schema in a
        // host's registration pass, which this fixture does not run.
        services.TryAddSingleton<IFileSystemConnectionFactory, FileSystemConnectionFactory>();
            services.TryAddSingleton<IImplementationServiceProvider<IGenericConnection, IConnectionImplementationConfiguration>>(
                sp => new FileSystemConnectionProvider(sp.GetRequiredService<IFileSystemConnectionFactory>()));
        services.TryAddSingleton(ConfigurationSchemaLoader.Load(schemaPath));
        services.TryAddSingleton<IConfigurationGatewayProvider>(sp =>
        {
            var gateways = new ConfigurationGatewayProvider();
            gateways.Register(new ConfigurationGateway(
                "PlatformConfiguration",
                sp.GetRequiredService<IImplementationServiceProvider<IGenericConnection, IConnectionImplementationConfiguration>>(),
                sp.GetRequiredService<ConfigurationSchema>(),
                sp.GetService<ILogger<ConfigurationGateway>>()));
            return gateways;
        });
        // Why: registered explicitly because FDW no longer ships a SecretManager [Implementation].
        // The concrete types (EnvironmentVariable, AzureKeyVault, MsSql, Sqlite, UserSecrets) moved to
        // reference-servicetypes, and each of them calls this as the first line of its
        // Register. With no option left in this repo, SecretManagerTypes.Register below
        // iterates an EMPTY collection and silently registers nothing — which is how the header provider
        // came to be missing and these tests started failing with "No service for type
        // SecretManagerConfigurationProvider". FDW's own tests cannot reference reference-servicetypes to
        // get it back: that package depends on FDW, so the dependency only runs one way.
        // TryAddSingleton inside makes this idempotent if an option is ever present again.

        // The SecretManager domain, and the one implementation this fixture's seed rows name. Registered
        // here because FDW ships no SecretManager implementation; the concrete ones live in this repo.
        // Why factory lambdas and not TryAddSingleton<T>(): both providers derive from
        // DomainConfigurationProviderBase<T>/ImplementationProviderBase<T,TContract>, whose constructor
        // takes a dataStoreName string alongside the DI-resolvable dependencies -- ActivatorUtilities
        // cannot supply a raw string, so a parameterless TryAddSingleton<T>() fails to activate.
        services.TryAddSingleton(sp => new SecretManagerConfigurationProvider(
            sp.GetRequiredService<ILogger<SecretManagerConfigurationProvider>>(),
            sp.GetRequiredService<IConfigurationGatewayProvider>(),
            "PlatformConfiguration", "sec", "SecretManager"));
        services.TryAddSingleton<ISecretManagerConfigurationProvider>(
            sp => sp.GetRequiredService<SecretManagerConfigurationProvider>());
        services.TryAddSingleton(sp => new EnvironmentVariableSecretManagerConfigurationProvider(
            sp.GetRequiredService<ILogger<EnvironmentVariableSecretManagerConfigurationProvider>>(),
            sp.GetRequiredService<IConfigurationGatewayProvider>(),
            "PlatformConfiguration", "sec", "EnvironmentVariableSecretManagerConfiguration"));
        services.TryAddSingleton<IEnvironmentVariableSecretManagerConfigurationProvider>(
            sp => sp.GetRequiredService<EnvironmentVariableSecretManagerConfigurationProvider>());

        // Why: consumers -- the connection factories above all -- depend on ISecretManagerProvider, and this
        // forward hands them the SAME instance the generated registration built, so its factory registrations
        // and cache are shared. The cast is deliberate and fails loud if that registration is ever replaced.
        services.TryAddSingleton<ISecretManagerProvider>(
            sp => (ISecretManagerProvider)sp.GetRequiredService<IDomainServiceProvider<ISecretManager, ISecretManagerImplementationConfiguration>>());

        // Why: kept so that if a SecretManager option is reintroduced to this repo its factory and
        // typed-body provider still get wired; today it is a no-op over an empty collection.
        SecretManagerTypes.Register(builder, null);

        // Why the host and not services.BuildServiceProvider(): phase 3 takes the host now, and building
        // the container off the builder keeps this fixture on the same construction path a real host
        // uses — which is exactly what the resolution note below depends on.
        _host = builder.Build();
        Provider = _host.Services;
        SecretManagerTypes.Initialize(_host, null);

        // Why: SecretManagerTypes' generated provider is registered AddScoped, so
        // RegisterFactory (which calls EnvironmentVariableSecretManagerType.RegisterFactory ->
        // SecretManagerConfigurationProvider.Register) only runs the FIRST TIME
        // IDomainServiceProvider<ISecretManager, ISecretManagerImplementationConfiguration> is actually resolved — it is
        // NOT run by Initialize() for the scoped path. A real host resolves this provider per-request
        // via an ISecretManager consumer; this test forces that same resolution once so the typed-body
        // provider is wired before any Get(...) call reads an implementation through the domain.
        Provider.GetRequiredService<IDomainServiceProvider<ISecretManager, ISecretManagerImplementationConfiguration>>();

        // The implementation is handed to its domain, the step every SecretManager option performs in its
        // own Initialization. The name matches the Implementation the seeded domain row records.
        Provider.GetRequiredService<SecretManagerConfigurationProvider>()
            .Register("EnvironmentVariable", Provider.GetRequiredService<IEnvironmentVariableSecretManagerConfigurationProvider>());
    }

    // Why: clone the committed seed into the isolated temp root so writes never touch the bin-copied fixtures.
    private static void CopySeed(string source, string destination)
    {
        foreach (var dir in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dir.Replace(source, destination, StringComparison.Ordinal));
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            File.Copy(file, file.Replace(source, destination, StringComparison.Ordinal), overwrite: true);
    }

    // Why: the committed schema pins Root to the relative "config-data" seed folder; clone it and point
    // Root at the isolated temp copy so the gateway reads/writes the throwaway store.
    private static string WriteSchemaPointingAt(string root)
    {
        var source = Path.Combine(AppContext.BaseDirectory, "configurationSchema.json");
        var node = JsonNode.Parse(File.ReadAllText(source))!;
        node["ConfigurationSchema"]!["Connections"]!.AsArray()[0]!["Configuration"]!["Root"] = root;
        var target = Path.Combine(root, "configurationSchema.json");
        File.WriteAllText(target, node.ToJsonString());
        return target;
    }

    public void Dispose()
    {
        _host.Dispose();
        Directory.Delete(Root, recursive: true);
    }

}
