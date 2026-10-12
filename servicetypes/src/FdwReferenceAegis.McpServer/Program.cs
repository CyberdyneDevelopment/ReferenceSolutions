using System;
using System.IO;
using System.Threading.Tasks;
using Fdw.Aegis;
using Fdw.Aegis.Logging;
using Fdw.Aegis.McpServer;
using Fdw.Results;
using Fdw.Services.Data;
using Fdw.Services.Data.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace FdwReferenceAegis.McpServer;

/// <summary>
/// Entry point for the Aegis Gateway stdio MCP server. Every row it reads - the declared commands and the
/// secret managers - comes from the declared connection the schema names, not from a store this host owns.
/// </summary>
public static class Program
{
    private const string SchemaFileName = "aegisSchema.json";

    // aegisSchema.json declares the one connection holding this host's configuration rows, a FileSystem connection
    // over the config-data folder that ships beside the executable, as both ConnectionRoles.Platform and
    // ConnectionRoles.Server; the registration reads that declaration, so no name is written here.

    /// <summary>Runs the stdio MCP server.</summary>
    /// <param name="args">The command-line arguments, handed to the host builder.</param>
    /// <returns>Zero after a clean shutdown; one when startup failed.</returns>
    public static async Task<int> Main(string[] args)
    {
        // The schema declares the store's Root relative to the install. A stdio tool is launched from the
        // caller's working directory, so the process is anchored to where it was installed before anything
        // resolves a relative path.
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);

        var builder = Host.CreateApplicationBuilder(args);

        if (!builder.Configuration.GetSection("Serilog").Exists())
        {
            // NO FALLBACKS: appsettings.json ships alongside this app and declares the sink and the
            // levels. A missing section is a deployment defect - fail loud rather than inventing a
            // default log path. Written to stderr because this runs before any logger exists, and
            // stderr is the safe channel (stdout is the protocol).
            return await Fail("required 'Serilog' configuration section is missing from appsettings.json.").ConfigureAwait(false);
        }

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(
            new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger(),
            dispose: true);

        // Why each result is checked: a phase that fails returns a coded failure, and continuing past it builds
        // a host whose configuration cannot be read - which surfaces later as a secret that cannot be resolved,
        // far from the cause. Failing here names the phase that actually broke.
        var schema = ConfigurationSchemaLoader.Load(SchemaFileName);
        var registered = Register(builder, schema);
        if (registered.IsFailure)
        {
            return await Fail("host registration failed: " + registered.CurrentMessage).ConfigureAwait(false);
        }

        var app = builder.Build();

        var initialized = Initialize(app);
        if (initialized.IsFailure)
        {
            return await Fail("host registration failed: " + initialized.CurrentMessage).ConfigureAwait(false);
        }

        return await Serve(app, schema.Connections.Count).ConfigureAwait(false);
    }

    private static IGenericResult<IHostApplicationBuilder> Register(HostApplicationBuilder builder, ConfigurationSchema schema)
    {
        var configurationSource = AegisHostRegistration.RegisterConfigurationSource(
            builder, SchemaFileName, loggerFactory: null);
        if (configurationSource.IsFailure)
        {
            return configurationSource;
        }

        var configured = AegisHostRegistration.Configure(builder, loggerFactory: null);
        if (configured.IsFailure)
        {
            return configured;
        }

        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<AegisToolService>();

        return AegisHostRegistration.Register(builder, schema, loggerFactory: null);
    }

    private static IGenericResult<IHost> Initialize(IHost app)
    {
        var sourceInitialized = AegisHostRegistration.InitializeConfigurationSource(app, loggerFactory: null);
        return sourceInitialized.IsFailure
            ? sourceInitialized
            : AegisHostRegistration.Initialize(app, loggerFactory: null);
    }

    private static async Task<int> Serve(IHost app, int connectionCount)
    {
        // The declared commands are the AegisCommand domain. Reading them here fails the start the same way a
        // failed phase does, rather than serving tools over a store that cannot be read.
        var commands = await app.Services.GetRequiredService<IAegisCommandConfigurationProvider>()
            .Get().ConfigureAwait(false);
        if (!commands.IsSuccess)
        {
            return await Fail("the declared commands could not be read: " + commands.CurrentMessage).ConfigureAwait(false);
        }

        AegisLog.ServerReady(
            app.Services.GetRequiredService<ILogger<AegisToolService>>(),
            commands.Value!.Count,
            connectionCount);

        await app.RunAsync().ConfigureAwait(false);
        return 0;
    }

    private static async Task<int> Fail(string reason)
    {
        await Console.Error.WriteLineAsync("Aegis MCP server: " + reason).ConfigureAwait(false);
        return 1;
    }
}
