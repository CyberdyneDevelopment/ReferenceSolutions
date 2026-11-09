using Fdw.Aegis;
using Fdw.Aegis.McpServer;
using Fdw.Results;
using Fdw.Services.Data;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FdwReferenceAegis.McpServer.Tests;

/// <summary>
/// Builds the Aegis host's registration graph once, through the same phases <c>Program</c> drives and over the
/// schema and rows the host ships, so what the tests read is what the deployed tool reads.
/// </summary>
public sealed class DeclaredConfigurationSourceFixture : IDisposable
{
    private const string SchemaFileName = "aegisSchema.json";

    private readonly IHost _host;

    public DeclaredConfigurationSourceFixture()
    {
        // The schema declares the store's Root relative to the install, as the host anchors it.
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);

        var builder = Host.CreateApplicationBuilder();

        // Why stated here: the connection registration builds the disabled-state service for every host, and that service takes the policy
        // that decides whether one tenant may change another's object (FDW-4459). Only the settings service type registers the real one, and
        // this host has no tenants and never enables a connection, so the policy is never asked.
        builder.Services.AddSingleton<ICrossTenantChangePolicy, UnaskedCrossTenantChangePolicy>();

        AegisHostRegistration.RegisterConfigurationSource(builder, SchemaFileName)
            .IsSuccess.ShouldBeTrue();
        AegisHostRegistration.Configure(builder).IsSuccess.ShouldBeTrue();
        AegisHostRegistration.Register(builder, ConfigurationSchemaLoader.Load(SchemaFileName)).IsSuccess.ShouldBeTrue();

        _host = builder.Build();

        AegisHostRegistration.InitializeConfigurationSource(_host).IsSuccess.ShouldBeTrue();
        AegisHostRegistration.Initialize(_host).IsSuccess.ShouldBeTrue();
    }

    public IAegisCommandConfigurationProvider Commands => _host.Services.GetRequiredService<IAegisCommandConfigurationProvider>();

    public void Dispose() => _host.Dispose();

    private sealed class UnaskedCrossTenantChangePolicy : ICrossTenantChangePolicy
    {
        public IGenericResult Permit(string changeName, Guid ownerTenantId, Guid? actorTenantId) => GenericResult.Success();
    }
}
