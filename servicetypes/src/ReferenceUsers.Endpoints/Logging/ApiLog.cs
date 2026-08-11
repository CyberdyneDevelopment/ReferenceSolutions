using System.Diagnostics.CodeAnalysis;
using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace ReferenceUsers.Endpoints.Logging;

/// <summary>
/// MessageLogging definitions for generic API operations.
/// EventId range: 5700-5799
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class ApiLog
{
    // Generic fetch operations (5700-5709)
    [MessageLogging(EventId = 5700, Level = LogLevel.Information, Message = "Fetching {resourceType}")]
    public static partial IGenericMessage FetchingData(ILogger logger, string resourceType);

    [MessageLogging(EventId = 5701, Level = LogLevel.Information, Message = "Retrieved {count} {resourceType}")]
    public static partial IGenericMessage DataRetrieved(ILogger logger, string resourceType, int count);

    [MessageLogging(EventId = 5702, Level = LogLevel.Error, Message = "Failed to fetch {resourceType}: {message}")]
    public static partial IGenericMessage DataFetchFailed(ILogger logger, string resourceType, string message);

    [MessageLogging(EventId = 5703, Level = LogLevel.Warning, Message = "{resourceType} not found")]
    public static partial IGenericMessage ResourceNotFound(ILogger logger, string resourceType);

    // Generic create operations (5710-5719)
    [MessageLogging(EventId = 5710, Level = LogLevel.Information, Message = "Creating {resourceType}")]
    public static partial IGenericMessage CreatingResource(ILogger logger, string resourceType);

    [MessageLogging(EventId = 5711, Level = LogLevel.Information, Message = "{resourceType} created successfully")]
    public static partial IGenericMessage ResourceCreated(ILogger logger, string resourceType);

    [MessageLogging(EventId = 5712, Level = LogLevel.Error, Message = "Failed to create {resourceType}: {message}")]
    public static partial IGenericMessage CreateFailed(ILogger logger, string resourceType, string message);

    // Generic update operations (5720-5729)
    [MessageLogging(EventId = 5720, Level = LogLevel.Information, Message = "Updating {resourceType}")]
    public static partial IGenericMessage UpdatingResource(ILogger logger, string resourceType);

    [MessageLogging(EventId = 5721, Level = LogLevel.Information, Message = "{resourceType} updated successfully")]
    public static partial IGenericMessage ResourceUpdated(ILogger logger, string resourceType);

    [MessageLogging(EventId = 5722, Level = LogLevel.Error, Message = "Failed to update {resourceType}: {message}")]
    public static partial IGenericMessage UpdateFailed(ILogger logger, string resourceType, string message);

    // Generic delete operations (5730-5739)
    [MessageLogging(EventId = 5730, Level = LogLevel.Information, Message = "Deleting {resourceType}")]
    public static partial IGenericMessage DeletingResource(ILogger logger, string resourceType);

    [MessageLogging(EventId = 5731, Level = LogLevel.Information, Message = "{resourceType} deleted successfully")]
    public static partial IGenericMessage ResourceDeleted(ILogger logger, string resourceType);

    [MessageLogging(EventId = 5732, Level = LogLevel.Error, Message = "Failed to delete {resourceType}: {message}")]
    public static partial IGenericMessage DeleteFailed(ILogger logger, string resourceType, string message);

    // Catalog search operations (5740-5749)
    [MessageLogging(EventId = 5740, Level = LogLevel.Error, Message = "Catalog search failed for {entityType}: {message}")]
    public static partial IGenericMessage CatalogSearchFailed(ILogger logger, string entityType, string message);

    // Execution operations (5750-5759)
    [MessageLogging(EventId = 5750, Level = LogLevel.Information, Message = "{operation} completed: {count} items processed")]
    public static partial IGenericMessage ExecutionCompleted(ILogger logger, string operation, int count);

    [MessageLogging(EventId = 5751, Level = LogLevel.Error, Message = "{operation} failed: {message}")]
    public static partial IGenericMessage ExecutionFailed(ILogger logger, string operation, string message);

    // Quality check operations (5760-5769)
    [MessageLogging(EventId = 5760, Level = LogLevel.Information, Message = "Quality check '{ruleId}' completed: {status} ({failureCount}/{totalCount} failures)")]
    public static partial IGenericMessage QualityCheckCompleted(ILogger logger, Guid ruleId, string status, int failureCount, int totalCount);

    // Additional generic operations (5770-5799)
    [MessageLogging(EventId = 5770, Level = LogLevel.Information, Message = "Getting {resourceType} for {category}")]
    public static partial IGenericMessage GettingResourceForCategory(ILogger logger, string resourceType, string category);

    [MessageLogging(EventId = 5771, Level = LogLevel.Debug, Message = "Found {count} {resourceType}")]
    public static partial IGenericMessage ResourceCountRetrieved(ILogger logger, int count, string resourceType);

    [MessageLogging(EventId = 5772, Level = LogLevel.Information, Message = "Getting {resourceType} detail for {category}/{type}")]
    public static partial IGenericMessage GettingResourceDetail(ILogger logger, string resourceType, string category, string type);

    [MessageLogging(EventId = 5773, Level = LogLevel.Warning, Message = "{resourceType} not found: {category}/{type}")]
    public static partial IGenericMessage ResourceDetailNotFound(ILogger logger, string resourceType, string category, string type);

    [MessageLogging(EventId = 5774, Level = LogLevel.Information, Message = "Getting {resourceType}")]
    public static partial IGenericMessage GettingResource(ILogger logger, string resourceType);

    [MessageLogging(EventId = 5775, Level = LogLevel.Information, Message = "Getting {resourceType} for parent: {parent}")]
    public static partial IGenericMessage GettingResourceForParent(ILogger logger, string resourceType, string parent);

    [MessageLogging(EventId = 5776, Level = LogLevel.Information, Message = "Listing {resourceType}, category filter: {category}")]
    public static partial IGenericMessage ListingResourcesWithFilter(ILogger logger, string resourceType, string category);

    [MessageLogging(EventId = 5777, Level = LogLevel.Information, Message = "Getting {resourceType} {category}/{name}")]
    public static partial IGenericMessage GettingResourceByCategoryAndName(ILogger logger, string resourceType, string category, string name);

    [MessageLogging(EventId = 5778, Level = LogLevel.Warning, Message = "No {resourceType} found for category {category}")]
    public static partial IGenericMessage NoResourcesFoundForCategory(ILogger logger, string resourceType, string category);

    [MessageLogging(EventId = 5779, Level = LogLevel.Warning, Message = "Failed to query table {table} for {operation}")]
    public static partial IGenericMessage TableQueryFailed(ILogger logger, Exception exception, string table, string operation);

    [MessageLogging(EventId = 5780, Level = LogLevel.Warning, Message = "{resourceType} {name} not found in category {category}")]
    public static partial IGenericMessage ResourceNotFoundInCategory(ILogger logger, string resourceType, string name, string category);

    [MessageLogging(EventId = 5781, Level = LogLevel.Information, Message = "Creating {resourceType} {category}/{serviceType}/{name}")]
    public static partial IGenericMessage CreatingResourceWithType(ILogger logger, string resourceType, string category, string serviceType, string name);

    [MessageLogging(EventId = 5782, Level = LogLevel.Warning, Message = "{resourceType} type not found: {category}/{serviceType}")]
    public static partial IGenericMessage ResourceTypeNotFound(ILogger logger, string resourceType, string category, string serviceType);

    [MessageLogging(EventId = 5783, Level = LogLevel.Warning, Message = "{resourceType} {name} already exists")]
    public static partial IGenericMessage ResourceAlreadyExists(ILogger logger, string resourceType, string name);

    [MessageLogging(EventId = 5784, Level = LogLevel.Information, Message = "Updating {resourceType} {category}/{name}")]
    public static partial IGenericMessage UpdatingResourceByCategoryAndName(ILogger logger, string resourceType, string category, string name);

    [MessageLogging(EventId = 5785, Level = LogLevel.Warning, Message = "{resourceType} {name} not found")]
    public static partial IGenericMessage ResourceNotFoundByName(ILogger logger, string resourceType, string name);

    [MessageLogging(EventId = 5786, Level = LogLevel.Information, Message = "Deleting {resourceType} {category}/{name}")]
    public static partial IGenericMessage DeletingResourceByCategoryAndName(ILogger logger, string resourceType, string category, string name);

    [MessageLogging(EventId = 5787, Level = LogLevel.Information, Message = "Executing search: {query}")]
    public static partial IGenericMessage ExecutingSearch(ILogger logger, string query);

    [MessageLogging(EventId = 5788, Level = LogLevel.Warning, Message = "Failed to search {resourceType} in database")]
    public static partial IGenericMessage SearchFailed(ILogger logger, Exception exception, string resourceType);

    [MessageLogging(EventId = 5789, Level = LogLevel.Information, Message = "Search completed: {count} results in {durationMs}ms")]
    public static partial IGenericMessage SearchCompleted(ILogger logger, int count, double durationMs);

    [MessageLogging(EventId = 5790, Level = LogLevel.Information, Message = "Building dataflow graph")]
    public static partial IGenericMessage BuildingDataflowGraph(ILogger logger);

    [MessageLogging(EventId = 5791, Level = LogLevel.Information, Message = "Built dataflow graph with {nodeCount} nodes and {edgeCount} edges")]
    public static partial IGenericMessage DataflowGraphBuilt(ILogger logger, int nodeCount, int edgeCount);

    [MessageLogging(EventId = 5792, Level = LogLevel.Information, Message = "Getting lineage for DataSet: {name}")]
    public static partial IGenericMessage GettingDataSetLineage(ILogger logger, string name);

    [MessageLogging(EventId = 5793, Level = LogLevel.Information, Message = "Analyzing impact for {targetType}: {targetName}")]
    public static partial IGenericMessage AnalyzingImpact(ILogger logger, string targetType, string targetName);
}
