using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Scheduling;
using Fdw.Services.Scheduling.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FdwReferenceSchedulers.EtlDispatch.Registration;

/// <summary>
/// Registers <see cref="EtlDispatchExecutionHandler"/> as the scheduled execution handler (FDW-902).
/// </summary>
/// <remarks>
/// <para>
/// Referencing this package is the registration: its module initializer declares the option, and the
/// three-phase lifecycle runs this Registration. A host names no handler in Program.cs.
/// </para>
/// <para>
/// Why scoped: Quartz's DI job factory resolves each fire in its own scope, and the handler depends on
/// <see cref="Fdw.Services.Pipelines.Clients.Abstractions.IPipelineJobClient"/>, which FDW's
/// PipelineJobClient API client type registers scoped against the <c>PipelineJobClient</c> HTTP connection.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[Implementation(typeof(ScheduledExecutionHandlerTypes), "EtlDispatch")]
public sealed class EtlDispatchExecutionHandlerType : ScheduledExecutionHandlerTypeBase<EtlDispatchExecutionHandler>
{
    /// <summary>Initializes a new instance of the <see cref="EtlDispatchExecutionHandlerType"/> class.</summary>
    public EtlDispatchExecutionHandlerType()
        : base("EtlDispatch", "ETL Dispatch")
    {
        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddScoped<IGenericScheduledExecutionHandler, EtlDispatchExecutionHandler>();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
