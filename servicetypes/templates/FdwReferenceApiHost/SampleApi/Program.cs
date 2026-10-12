using System.Threading.Tasks;
using Fdw.Services.Data;
using Fdw.Services.Data.Configuration;
using Fdw.ServiceTypes;
// Why this using: the per-domain accessors (PlatformServices.Logging, .Connection, .SecretManager,
// .ConfigurationGateway, .Endpoints) are extension members the registration generator emits into
// <RootNamespace>.Generated. Without it PlatformServices has only the three phase methods and every
// named phase fails to compile.
using SampleApi.Generated;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace SampleApi;

/// <summary>
/// The host. Read this top to bottom — the order below is load-bearing.
/// </summary>
public static class Program
{
    /// <summary>Entry point.</summary>
    public static async Task<int> Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        if (PlatformServices.Logging.Configure(builder, null).IsFailure) return 1;

        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());

        // ── 1. What the rest of registration reads through ───────────────────────────────────
        // configurationSchema.json declares only what this host needs to REACH its configuration:
        // ServerConfiguration, the host's own boot-time rows over a FileSystem connection, and
        // PlatformConfiguration, the shared store. Every other connection is runtime data inside those
        // stores, read later through the domain providers.
        //
        // These three run ahead of the rest, in this order. The gateway builds both of its connections
        // while it registers, so the connection kinds' factories must already exist, and building a
        // connection resolves the secret manager provider, so its registry must exist before that.
        // PlatformServices holds domains in the order their module initializers attached them and never
        // reorders them — ordering domains against each other is the host's call. Each result is checked
        // here because a phase that ran and failed is still marked as run, and the bulk Register below
        // would skip straight past it.
        if (PlatformServices.Connection.Register(builder, loggerFactory).IsFailure) return 1;
        if (PlatformServices.SecretManager.Register(builder, loggerFactory).IsFailure) return 1;
        if (PlatformServices.ConfigurationGateway.Register(builder, loggerFactory).IsFailure) return 1;

        // ── 2. Three-phase service registration ──────────────────────────────────────────────
        // Configure → Register → Initialize, in that order, each returning a result rather than
        // throwing.
        if (PlatformServices.Configure(builder, loggerFactory).IsFailure) return 1;
        if (PlatformServices.Logging.Register(builder, loggerFactory).IsFailure) return 1;
        if (PlatformServices.Register(builder, loggerFactory).IsFailure) return 1;

        var app = builder.Build();

        // ── 3. Initialize, against the built host ────────────────────────────────────────────
        // The phase that needs a live container: providers are resolved, endpoints are mapped, and
        // anything that cannot be satisfied stops the host here rather than failing per-request.
        //
        // There is no system-identity scope around this block. Each domain that reads tenant-scoped
        // configuration while it initializes opens and closes its own, so no host has to remember to.
        //
        // The secret manager first: everything that follows reads configuration through it.
        if (PlatformServices.SecretManager.Initialize(app, loggerFactory).IsFailure) return 1;

        // Endpoints twice, deferred then not: the first pass maps routes so a domain that resolves one
        // during its own Initialize finds it; the second completes them.
        PlatformServices.Endpoints.Initialize(app, loggerFactory, defer: true);

        if (PlatformServices.Initialize(app, loggerFactory).IsFailure) return 1;
        if (PlatformServices.Endpoints.Initialize(app, loggerFactory).IsFailure) return 1;

        await app.RunAsync().ConfigureAwait(false);
        return 0;
    }
}
