using System.Diagnostics.CodeAnalysis;
using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Logging;

/// <summary>
/// MessageLogging definitions for DataStore management endpoints.
/// EventId range: 5300-5399
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class DataStoreLog
{
    // List operations (5300-5309)
    [MessageLogging(EventId = 5300, Level = LogLevel.Information, Message = "Listing DataStores")]
    public static partial IGenericMessage ListingDataStores(ILogger logger);

    [MessageLogging(EventId = 5301, Level = LogLevel.Information, Message = "Found {count} DataStores")]
    public static partial IGenericMessage DataStoresFound(ILogger logger, int count);

    [MessageLogging(EventId = 5302, Level = LogLevel.Error, Message = "Failed to list DataStores: {message}")]
    public static partial IGenericMessage ListDataStoresFailed(ILogger logger, string message);

    // Read operations (5310-5319)
    [MessageLogging(EventId = 5310, Level = LogLevel.Information, Message = "Fetching DataStore '{name}'")]
    public static partial IGenericMessage FetchingDataStore(ILogger logger, string name);

    [MessageLogging(EventId = 5311, Level = LogLevel.Information, Message = "DataStore '{name}' retrieved with {pathCount} paths")]
    public static partial IGenericMessage DataStoreRetrieved(ILogger logger, string name, int pathCount);

    [MessageLogging(EventId = 5312, Level = LogLevel.Warning, Message = "DataStore '{name}' not found")]
    public static partial IGenericMessage DataStoreNotFound(ILogger logger, string name);

    [MessageLogging(EventId = 5313, Level = LogLevel.Error, Message = "Failed to fetch DataStore '{name}': {message}")]
    public static partial IGenericMessage DataStoreFetchFailed(ILogger logger, string name, string message);

    // Container operations (5320-5329)
    [MessageLogging(EventId = 5320, Level = LogLevel.Information, Message = "Fetching container '{containerName}' in {storeName}/{pathName}")]
    public static partial IGenericMessage FetchingContainer(ILogger logger, string containerName, string storeName, string pathName);

    [MessageLogging(EventId = 5321, Level = LogLevel.Information, Message = "Container '{containerName}' retrieved with {fieldCount} fields")]
    public static partial IGenericMessage ContainerRetrieved(ILogger logger, string containerName, int fieldCount);

    [MessageLogging(EventId = 5322, Level = LogLevel.Warning, Message = "Container '{containerName}' not found in {storeName}/{pathName}")]
    public static partial IGenericMessage ContainerNotFound(ILogger logger, string containerName, string storeName, string pathName);

    [MessageLogging(EventId = 5323, Level = LogLevel.Information, Message = "Adding container '{containerName}'")]
    public static partial IGenericMessage AddingContainer(ILogger logger, string containerName);

    [MessageLogging(EventId = 5324, Level = LogLevel.Warning, Message = "Container '{containerName}' already exists")]
    public static partial IGenericMessage ContainerAlreadyExists(ILogger logger, string containerName);

    [MessageLogging(EventId = 5325, Level = LogLevel.Information, Message = "Container '{containerName}' added successfully")]
    public static partial IGenericMessage ContainerAddedSuccessfully(ILogger logger, string containerName);

    // Path operations (5330-5339)
    [MessageLogging(EventId = 5330, Level = LogLevel.Information, Message = "Fetching paths for DataStore '{name}'")]
    public static partial IGenericMessage FetchingPaths(ILogger logger, string name);

    [MessageLogging(EventId = 5331, Level = LogLevel.Information, Message = "Found {count} paths for DataStore '{name}'")]
    public static partial IGenericMessage PathsRetrieved(ILogger logger, string name, int count);

    [MessageLogging(EventId = 5332, Level = LogLevel.Information, Message = "Adding path '{pathName}' to DataStore '{storeName}'")]
    public static partial IGenericMessage AddingPath(ILogger logger, string pathName, string storeName);

    [MessageLogging(EventId = 5333, Level = LogLevel.Warning, Message = "Path '{pathName}' already exists in DataStore '{storeName}'")]
    public static partial IGenericMessage PathAlreadyExists(ILogger logger, string pathName, string storeName);

    [MessageLogging(EventId = 5334, Level = LogLevel.Information, Message = "Path '{pathName}' added successfully to DataStore '{storeName}'")]
    public static partial IGenericMessage PathAddedSuccessfully(ILogger logger, string pathName, string storeName);

    [MessageLogging(EventId = 5335, Level = LogLevel.Error, Message = "Failed to add path '{pathName}' to DataStore '{storeName}': {message}")]
    public static partial IGenericMessage AddPathFailed(ILogger logger, string pathName, string storeName, string? message);

    // Update operations (5340-5349)
    [MessageLogging(EventId = 5340, Level = LogLevel.Information, Message = "Updating DataStore '{name}'")]
    public static partial IGenericMessage UpdatingDataStore(ILogger logger, string name);

    [MessageLogging(EventId = 5341, Level = LogLevel.Error, Message = "Failed to update DataStore '{name}': {message}")]
    public static partial IGenericMessage DataStoreUpdateFailed(ILogger logger, string name, string message);

    [MessageLogging(EventId = 5342, Level = LogLevel.Information, Message = "DataStore '{name}' updated successfully")]
    public static partial IGenericMessage DataStoreUpdated(ILogger logger, string name);

    // Container list operations (5350-5359)
    [MessageLogging(EventId = 5350, Level = LogLevel.Information, Message = "Listing containers for DataStore '{name}'")]
    public static partial IGenericMessage ListingContainers(ILogger logger, string name);

    [MessageLogging(EventId = 5351, Level = LogLevel.Information, Message = "Listed {count} containers for DataStore '{name}'")]
    public static partial IGenericMessage ContainersListed(ILogger logger, int count, string name);

    [MessageLogging(EventId = 5352, Level = LogLevel.Information, Message = "Fetching container by ID '{id}'")]
    public static partial IGenericMessage FetchingContainerById(ILogger logger, Guid id);

    // Create operations (5360-5369)
    [MessageLogging(EventId = 5360, Level = LogLevel.Information, Message = "Creating DataStore '{name}'")]
    public static partial IGenericMessage CreatingDataStore(ILogger logger, string name);

    [MessageLogging(EventId = 5361, Level = LogLevel.Warning, Message = "DataStore '{name}' already exists")]
    public static partial IGenericMessage DataStoreAlreadyExists(ILogger logger, string name);

    [MessageLogging(EventId = 5362, Level = LogLevel.Error, Message = "Failed to create DataStore '{name}': {message}")]
    public static partial IGenericMessage DataStoreCreateFailed(ILogger logger, string name, string message);

    [MessageLogging(EventId = 5363, Level = LogLevel.Information, Message = "DataStore '{name}' created successfully")]
    public static partial IGenericMessage DataStoreCreated(ILogger logger, string name);

    // Delete operations (5370-5379)
    [MessageLogging(EventId = 5370, Level = LogLevel.Information, Message = "Deleting DataStore '{name}'")]
    public static partial IGenericMessage DeletingDataStore(ILogger logger, string name);

    [MessageLogging(EventId = 5371, Level = LogLevel.Error, Message = "Failed to delete DataStore '{name}': {message}")]
    public static partial IGenericMessage DataStoreDeleteFailed(ILogger logger, string name, string message);

    [MessageLogging(EventId = 5372, Level = LogLevel.Information, Message = "DataStore '{name}' deleted successfully")]
    public static partial IGenericMessage DataStoreDeleted(ILogger logger, string name);

    // Discovery operations (5380-5389)
    [MessageLogging(EventId = 5380, Level = LogLevel.Information, Message = "Starting discovery for DataStore '{name}'")]
    public static partial IGenericMessage StartingDiscovery(ILogger logger, string name);

    [MessageLogging(EventId = 5381, Level = LogLevel.Error, Message = "Discovery failed for DataStore '{name}': {message}")]
    public static partial IGenericMessage DiscoveryFailed(ILogger logger, string name, string message);

    [MessageLogging(EventId = 5382, Level = LogLevel.Information, Message = "Discovery completed for DataStore '{name}': discovered {containerCount} containers with {fieldCount} fields")]
    public static partial IGenericMessage DiscoveryCompleted(ILogger logger, string name, int containerCount, int fieldCount);

    [MessageLogging(EventId = 5364, Level = LogLevel.Error, Message = "Cannot create DataStore '{name}': ConnectionName is required (the store's transport is inherited from its connection)")]
    public static partial IGenericMessage ConnectionNameRequired(ILogger logger, string name);

    [MessageLogging(EventId = 5383, Level = LogLevel.Error, Message = "DataStore '{name}' has no ServiceOptionType configured")]
    public static partial IGenericMessage ServiceOptionTypeMissing(ILogger logger, string name);
}
