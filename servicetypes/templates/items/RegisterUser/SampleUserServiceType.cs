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
/// The phase bodies are set in the constructor with Registration and Initialization. This class
/// declares them, so it owns them outright. AppendRegistration, PrependRegistration and the Configuration
/// and Initialization counterparts exist for a host or package extending an option it did not author;
/// inside the declaring class they are the error STC001. Where a body needs the func another piece would
/// have run, capture that func in a local and call it at the point in the body where it belongs. No
/// base contributes a segment, because STC002 refuses a base that sets a phase: wiring every option of
/// a domain needs belongs in the collection's Register body.
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
        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAddScoped<ISampleService, SampleService>();

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // Initialize runs after Build(), so it is the only phase with a real IServiceProvider —
        // a resolved service, a middleware pipeline or a mapped route can only exist here.
        Initialization((host, loggerFactory) =>
        {
            return GenericResult<IHost>.Success(host);
        });
    }
}
