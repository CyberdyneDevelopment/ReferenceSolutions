using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace SampleNamespace;

/// <summary>
/// The SAMPLE_NAME member of the User service types.
/// </summary>
/// <remarks>
/// Referencing the package that contains this IS the registration: a module initializer adds it to
/// its collection at assembly load, before Main() runs.
///
/// Append rather than replace. AppendRegistration runs after whatever is already chained, so the
/// base's own body and any other contribution still run. Registration(...) assigns over all of it,
/// and nothing reports that it happened — the option simply stops doing part of its job.
///
/// Delete a phase this option does not need. The inherited default returns success, so an unset
/// phase costs nothing and says plainly that there is nothing to do.
/// </remarks>
[ExcludeFromCodeCoverage]
[Implementation(typeof(UserServiceTypes), "SAMPLE_NAME")]
public sealed class SampleUserServiceType : UserServiceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SampleUserServiceType"/> class.
    /// </summary>
    public SampleUserServiceType()
        : base("SAMPLE_NAME", "SAMPLE_NAME", "SAMPLE_NAME", "What this service type is.")
    {
        // Register runs while the container is still being built, so it can only add to it.
        AppendRegistration((builder, loggerFactory) =>
        {
            builder.Services.TryAddScoped<ISampleService, SampleService>();

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // Initialize runs after Build(), so it is the only phase with a real IServiceProvider —
        // a resolved service, a middleware pipeline or a mapped route can only exist here.
        AppendInitialization((host, loggerFactory) =>
        {
            return GenericResult<IHost>.Success(host);
        });
    }
}
