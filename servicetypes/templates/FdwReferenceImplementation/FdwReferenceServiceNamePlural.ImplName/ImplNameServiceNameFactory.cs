using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceServiceNamePlural.ImplName;

/// <summary>
/// Creates <see cref="ImplNameServiceName"/> instances from configuration.
/// </summary>
public sealed class ImplNameServiceNameFactory : IImplNameServiceNameFactory
{
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImplNameServiceNameFactory"/> class.
    /// </summary>
    /// <param name="loggerFactory">Supplies typed loggers to the services this factory creates.</param>
    public ImplNameServiceNameFactory(ILoggerFactory? loggerFactory)
        => _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;

    /// <summary>
    /// Creates the ImplName ServiceName.
    /// </summary>
    /// <returns>A configured service instance.</returns>
    public ImplNameServiceName Create(string name)
        => new(name, _loggerFactory.CreateLogger<ImplNameServiceName>());
}
