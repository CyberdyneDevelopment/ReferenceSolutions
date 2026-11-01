using Fdw.Aegis;
using Fdw.Aegis.McpServer;
using Fdw.Services.Data;
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
}
