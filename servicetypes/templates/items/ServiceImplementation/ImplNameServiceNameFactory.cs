using Fdw.Results;
using Fdw.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RootNamespace.ServiceName.Abstractions;

namespace RootNamespace.ServiceName.ImplName;

/// <summary>
/// Creates <see cref="ImplNameServiceNameService"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ServiceFactoryBase{TService, TConfiguration}"/> supplies the erased <c>Create</c>
/// overloads and the configuration-type check, so a factory writes only its typed <c>Create</c>.
/// </para>
/// <para>
/// A stateless singleton: dependencies come from DI through the constructor, configuration is passed
/// to each <c>Create</c>.
/// </para>
/// </remarks>
public sealed class ImplNameServiceNameFactory
    : ServiceFactoryBase<IServiceNameService, ImplNameServiceNameConfiguration>,
      IImplNameServiceNameFactory
{
    private readonly ILogger<ImplNameServiceNameService> _serviceLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImplNameServiceNameFactory"/> class.
    /// </summary>
    /// <param name="serviceLogger">The logger handed to created service instances.</param>
    public ImplNameServiceNameFactory(ILogger<ImplNameServiceNameService> serviceLogger)
        => _serviceLogger = serviceLogger ?? NullLogger<ImplNameServiceNameService>.Instance;

    /// <inheritdoc/>
    public override IGenericResult<IServiceNameService> Create(ImplNameServiceNameConfiguration configuration)
        => GenericResult<IServiceNameService>.Success(new ImplNameServiceNameService(_serviceLogger, configuration));
}
