using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceDataStores.Endpoints.Logging;

namespace ReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to add a container to an existing data store path.
/// </summary>
[ExcludeFromCodeCoverage]
public class AddDataStoreContainerEndpoint : AddDataStoreContainerEndpointBase
{
    private readonly ILogger<AddDataStoreContainerEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddDataStoreContainerEndpoint"/> class.
    /// </summary>
    public AddDataStoreContainerEndpoint(
        DataStoreConfigurationProvider dataStoreProvider,
        ILogger<AddDataStoreContainerEndpoint> logger)
        : base(dataStoreProvider)
    {
        _logger = logger ?? NullLogger<AddDataStoreContainerEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override void OnBeforeCreate(string resourceName)
    {
        DataStoreLog.AddingContainer(_logger, resourceName);
    }

    /// <inheritdoc />
    protected override void OnAlreadyExists(string resourceName)
    {
        DataStoreLog.ContainerAlreadyExists(_logger, resourceName);
    }

    /// <inheritdoc />
    protected override void OnAfterCreate(string resourceName)
    {
        DataStoreLog.ContainerAddedSuccessfully(_logger, resourceName);
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataStores");
    }
}
