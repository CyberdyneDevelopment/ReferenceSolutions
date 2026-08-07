# EtlServer Test Requirements Manifest

Generated: 2026-02-16
Project: `public/ReferenceSolutions/EtlServer/src/Reference.Etl.Server/`

---

## Table of Contents

1. [TriggerJobEndpoint](#triggerjobendpoint)
2. [GetJobStatusEndpoint](#getjobstatusendpoint)
3. [DefaultJobExecutionService](#defaultjobexecutionservice)
4. [HttpJobCompletionNotifier](#httpjobcompletionnotifier)
5. [NflApiClient](#nflapiclient)
6. [DemoPipeline](#demopipeline)
7. [NflApiPipeline](#nflapipipeline)
8. [JobTriggerSources TypeCollection](#jobtriggersources-typecollection)
9. [Trivial/Excluded Types](#trivialexcluded-types)

---

## TriggerJobEndpoint

**File:** `Endpoints/TriggerJobEndpoint.cs:22-68`
**Cyclomatic Complexity:** 3 (HandleAsync: null-coalesce on TriggerSource, if !result.IsSuccess, success path)
**Configure:** Complexity 1 (trivial)

### Dependencies to Mock

| Dependency | Type | Registration |
|------------|------|-------------|
| `IJobExecutionService` | Constructor-injected | Scoped |

### Branch Analysis

| Method | Line(s) | Branch | Description |
|--------|---------|--------|-------------|
| `HandleAsync` | 54 | `req.TriggerSource ?? "Api"` | Null-coalesce defaults TriggerSource to "Api" |
| `HandleAsync` | 55-60 | `if (!result.IsSuccess)` | Failure path: AddError + 400 |
| `HandleAsync` | 57 | `result.CurrentMessage ?? "Failed to trigger job"` | Null-coalesce on error message |
| `HandleAsync` | 62-66 | else (implicit) | Success path: 202 with ExecutionId |

### Test Cases

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `HandleAsyncReturns202WhenJobTriggeredSuccessfully` | Success path | Mock TriggerJob returns Success(Guid) | 202 Accepted, response has ExecutionId and Status="Triggered" |
| `HandleAsyncReturns400WhenJobTriggerFails` | Failure path (!IsSuccess) | Mock TriggerJob returns Failure with message | 400 with error message |
| `HandleAsyncReturns400WithDefaultMessageWhenCurrentMessageIsNull` | Null CurrentMessage | Mock TriggerJob returns Failure, CurrentMessage=null | 400 with "Failed to trigger job" default |
| `HandleAsyncDefaultsTriggerSourceToApiWhenNull` | Null TriggerSource | Request with TriggerSource=null | TriggerJob called with triggerSource="Api" |
| `HandleAsyncPassesTriggerSourceWhenProvided` | Explicit TriggerSource | Request with TriggerSource="Manual" | TriggerJob called with triggerSource="Manual" |
| `HandleAsyncPassesPipelineNameToService` | Parameter forwarding | Request with PipelineName="TestPipeline" | TriggerJob called with "TestPipeline" |
| `HandleAsyncPassesScheduleNameToService` | ScheduleName forwarding | Request with ScheduleName="Nightly" | TriggerJob called with scheduleName="Nightly" |
| `HandleAsyncPassesNullScheduleNameWhenNotProvided` | Null ScheduleName | Request without ScheduleName | TriggerJob called with scheduleName=null |

---

## GetJobStatusEndpoint

**File:** `Endpoints/GetJobStatusEndpoint.cs:14-46`
**Cyclomatic Complexity:** 3 (HandleAsync: !result.IsSuccess, result.Value is null, success)
**Configure:** Complexity 1 (trivial)

### Dependencies to Mock

| Dependency | Type | Registration |
|------------|------|-------------|
| `IJobExecutionService` | Constructor-injected | Scoped |

### Branch Analysis

| Method | Line(s) | Branch | Description |
|--------|---------|--------|-------------|
| `HandleAsync` | 38 | `!result.IsSuccess` | Service returned failure |
| `HandleAsync` | 38 | `result.Value is null` | Service returned success but null value |
| `HandleAsync` | 40-41 | Combined failure path | 404 NotFound |
| `HandleAsync` | 44 | Success path | 200 OK with JobStatusResponse |

### Test Cases

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `HandleAsyncReturns200WithStatusWhenJobExists` | Success path | Mock GetJobStatus returns Success(response) | 200 OK with JobStatusResponse |
| `HandleAsyncReturns404WhenServiceReturnsFailure` | !IsSuccess | Mock GetJobStatus returns Failure | 404 Not Found |
| `HandleAsyncReturns404WhenServiceReturnsNullValue` | IsSuccess but null Value | Mock GetJobStatus returns Success(null) | 404 Not Found |
| `HandleAsyncPassesExecutionIdToService` | Parameter forwarding | Request with specific Guid | GetJobStatus called with that Guid |
| `HandleAsyncPassesCancellationToken` | CancellationToken forwarding | Use TestContext.Current.CancellationToken | GetJobStatus receives the token |

---

## DefaultJobExecutionService

**File:** `Services/DefaultJobExecutionService.cs:24-300`
**Cyclomatic Complexity:** TriggerJob=6, ExecutePipeline=9, CompleteWithMetrics=2, GetJobStatus=5
**Total Testable Branches:** ~22

### Dependencies to Mock

| Dependency | Type | Interface |
|------------|------|-----------|
| `ILogger<DefaultJobExecutionService>` | Constructor | `Microsoft.Extensions.Logging.ILogger<T>` |
| `IFdwServiceProvider<IEtlPipeline, IEtlPipelineConfiguration>` | Constructor | Pipeline provider |
| `IExecutionTracker` | Constructor | Execution tracking |
| `IDataGateway` | Constructor | Data command routing |

### TriggerJob Method (Lines 49-134)

**Complexity:** 6

| Line(s) | Branch | Description |
|---------|--------|-------------|
| 69 | `!createResult.IsSuccess` | ExecutionTracker.CreateItem failed |
| 74 | `createResult.CurrentMessage ?? "Failed to create..."` | Null-coalesce on error message (x2 at lines 74, 76) |
| 96 | `!insertResult.IsSuccess` | DataGateway insert of ETL record failed (logged, not fatal) |
| 99-100 | `insertResult.CurrentMessage ?? "Insert failed"` | Null-coalesce on insert error (x2) |
| 107 | `!pipelineResult.IsSuccess \|\| pipelineResult.Value == null` | Pipeline not found |
| 109 | `pipelineResult.CurrentMessage ?? "Pipeline '...' not found"` | Null-coalesce on pipeline error |
| 128-131 | Fire-and-forget Task.Run | Background pipeline execution |
| 133 | Success path | Returns Success(executionId) |

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `TriggerJobReturnsSuccessWithExecutionId` | Happy path | All mocks succeed, pipeline exists | Success result with Guid |
| `TriggerJobReturnsFailureWhenCreateItemFails` | CreateItem failure | Mock CreateItem returns Failure | Failure result, logged via ExecutionRecordCreateFailed |
| `TriggerJobReturnsFailureWhenCreateItemFailsWithNullMessage` | CreateItem failure, null message | Mock CreateItem returns Failure(null message) | Failure result with default message |
| `TriggerJobLogsWarningWhenEtlRecordInsertFails` | Insert ETL record fails | Mock DataGateway.Execute returns Failure | Continues execution (non-fatal), logs warning |
| `TriggerJobLogsWarningWhenInsertFailsWithNullMessage` | Insert fails, null message | DataGateway returns Failure(null msg) | Logs with "Insert failed" default |
| `TriggerJobReturnsFailureWhenPipelineNotFound` | Pipeline not found | Mock pipelineProvider.Get returns Failure | Failure result, execution marked complete with "PipelineNotFound" |
| `TriggerJobReturnsFailureWhenPipelineGetReturnsNullValue` | Pipeline null | Mock pipelineProvider.Get returns Success(null) | Failure result, same as not found |
| `TriggerJobReturnsFailureWhenPipelineNotFoundWithNullMessage` | No message | pipelineProvider.Get returns Failure, null msg | Uses default "Pipeline '...' not found" |
| `TriggerJobCompletesExecutionItemOnPipelineNotFound` | Cleanup on failure | Pipeline not found | executionTracker.Complete called with success=false, resultCode="PipelineNotFound" |
| `TriggerJobDisposesValidationPipeline` | Resource cleanup | Pipeline exists | pipeline.Dispose() called on the validation pipeline |
| `TriggerJobQueuesBackgroundExecution` | Fire-and-forget | Pipeline exists | Task.Run is invoked (execution happens asynchronously) |
| `TriggerJobUsesUtcTimestampInExecutionName` | Name format | Any valid request | CreateItem name contains pipelineName and date |
| `TriggerJobPassesPipelineNameAndScheduleNameAsParameters` | Parameters | Request with scheduleName | CreateItem parameters include PipelineName and ScheduleName |
| `TriggerJobUsesApiTriggerSourceDefault` | TriggerSource | triggerSource="Api" | CreateItem called with triggerSource="Api" |

### ExecutePipeline Method (Lines 136-213) -- Private, tested indirectly

**Complexity:** 9 (try/catch x3, if/else on pipelineResult, if/else on executeResult with null checks)

| Line(s) | Branch | Description |
|---------|--------|-------------|
| 141-146 | Initial TransitionState | Always called on entry |
| 152 | `!pipelineResult.IsSuccess \|\| pipelineResult.Value == null` | Pipeline not found during execution |
| 168 | `executeResult.IsSuccess && executeResult.Value != null` | Pipeline succeeded with metrics |
| 186-201 | else (execute failed or null value) | Pipeline failed, metrics may be null |
| 195-198 | `metrics?.RecordsExtracted ?? 0` etc. | Null-coalescing on optional metrics |
| 203-206 | `catch (OperationCanceledException)` | Pipeline was cancelled |
| 208-212 | `catch (Exception ex)` | Unhandled exception |

These are tested indirectly through TriggerJob (which calls Task.Run -> ExecutePipeline). For thorough testing, we need to wait for background execution to complete.

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `ExecutePipelineTransitionsToRunningState` | State transition | Happy path trigger | TransitionState called with ExecutionStateTypes.Running |
| `ExecutePipelineCompletesWithSuccessMetrics` | Pipeline succeeds | Mock pipeline.Execute returns Success with metrics | CompleteWithMetrics called with success=true, actual metric values |
| `ExecutePipelineCompletesWithFailureWhenPipelineNotFound` | Pipeline disappears | First Get succeeds (validation), second Get fails | CompleteWithMetrics(false, "PipelineNotFound") |
| `ExecutePipelineCompletesWithFailureWhenExecuteFails` | Execute returns failure | pipeline.Execute returns Failure, Value=null | CompleteWithMetrics(false, "ExecutionFailed"), metrics defaulted to 0 |
| `ExecutePipelineCompletesWithFailureAndPartialMetrics` | Execute fails with partial metrics | pipeline.Execute returns Failure with non-null Value | CompleteWithMetrics uses actual metric values from partial result |
| `ExecutePipelineHandlesCancellation` | OperationCanceledException | pipeline.Execute throws OCE | executionTracker.Complete with "Cancelled", PipelineCancelled logged |
| `ExecutePipelineHandlesUnexpectedException` | General exception | pipeline.Execute throws Exception | CompleteWithMetrics(false, "Exception", ex.Message) |
| `ExecutePipelineDisposePipeline` | Resource cleanup | Happy path | pipeline.Dispose called (via using) |

### CompleteWithMetrics Method (Lines 215-255) -- Private

**Complexity:** 2 (if !updateResult.IsSuccess)

| Line(s) | Branch | Description |
|---------|--------|-------------|
| 228 | `success ? "Succeeded" : "Failed"` | Status string conditional |
| 244 | `!updateResult.IsSuccess` | DataGateway update fails |
| 247 | `updateResult.CurrentMessage ?? "Update failed"` | Null-coalesce on error |
| 254 | `success ? "Succeeded" : "Failed"` | Log message conditional |

Tested indirectly through ExecutePipeline.

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `CompleteWithMetricsUpdatesEtlRecordOnSuccess` | Success update | pipeline succeeds | DataGateway update with Status="Succeeded" |
| `CompleteWithMetricsUpdatesEtlRecordOnFailure` | Failure update | pipeline fails | DataGateway update with Status="Failed" |
| `CompleteWithMetricsLogsWarningWhenUpdateFails` | Update fails | DataGateway.Execute returns Failure | ExecutionRecordCompleteFailed logged, execution still completes |
| `CompleteWithMetricsCallsTrackerComplete` | Always | Any completion | executionTracker.Complete called |

### GetJobStatus Method (Lines 258-299)

**Complexity:** 5

| Line(s) | Branch | Description |
|---------|--------|-------------|
| 264 | `!itemResult.IsSuccess \|\| itemResult.Value == null` | Execution item not found |
| 279 | `metricsResult.IsSuccess ? FirstOrDefault(...) : null` | Metrics query success/failure |
| 283 | `item.StartedAt.HasValue && item.CompletedAt.HasValue` | Duration calculation |
| 290 | `metrics?.PipelineName ?? item.Name` | Fallback PipelineName |
| 292 | `item.StartedAt?.DateTime ?? item.CreatedAt.DateTime` | Fallback StartedAt |

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `GetJobStatusReturnsSuccessWhenItemExists` | Happy path | Mock returns valid item + metrics | Success with populated JobStatusResponse |
| `GetJobStatusReturnsFailureWhenItemNotFound` | GetItem failure | executionTracker.GetItem returns Failure | Failure, ExecutionRecordNotFound logged |
| `GetJobStatusReturnsFailureWhenItemIsNull` | GetItem returns null | executionTracker.GetItem returns Success(null) | Failure, ExecutionRecordNotFound logged |
| `GetJobStatusReturnsResponseWithoutMetricsWhenQueryFails` | Metrics query fails | DataGateway returns Failure | Success, uses item.Name for PipelineName, 0 for record counts |
| `GetJobStatusReturnsResponseWithoutMetricsWhenNoRecords` | No ETL record | DataGateway returns Success(empty list) | Success, metrics all default to 0 |
| `GetJobStatusCalculatesDurationFromTimestamps` | Both timestamps present | item.StartedAt and CompletedAt set | DurationMs calculated correctly |
| `GetJobStatusReturnsNullDurationWhenStartedAtMissing` | No StartedAt | item.StartedAt=null | DurationMs=null |
| `GetJobStatusReturnsNullDurationWhenCompletedAtMissing` | No CompletedAt | item.CompletedAt=null | DurationMs=null |
| `GetJobStatusUsesPipelineNameFromMetricsWhenAvailable` | Metrics has PipelineName | metrics.PipelineName="TestPipeline" | response.PipelineName="TestPipeline" |
| `GetJobStatusFallsBackToItemNameWhenMetricsMissing` | No metrics | metrics=null | response.PipelineName=item.Name |
| `GetJobStatusUsesStartedAtFromItemWhenAvailable` | Item has StartedAt | item.StartedAt set | response.StartedAt=item.StartedAt.DateTime |
| `GetJobStatusFallsBackToCreatedAtWhenStartedAtMissing` | No StartedAt | item.StartedAt=null | response.StartedAt=item.CreatedAt.DateTime |
| `GetJobStatusReturnsResultMessageAsErrorMessage` | Error message | item.ResultMessage="Some error" | response.ErrorMessage="Some error" |
| `GetJobStatusReturnsStateNameAsStatus` | State mapping | item.State.Name="Running" | response.Status="Running" |
| `GetJobStatusPassesCancellationToken` | Token forwarding | Use TestContext token | Both GetItem and DataGateway calls receive it |

---

## HttpJobCompletionNotifier

**File:** `Services/HttpJobCompletionNotifier.cs:27-59`
**Cyclomatic Complexity:** 4 (null/empty check, try, success path, catch)

### Dependencies to Mock

| Dependency | Type | Interface |
|------------|------|-----------|
| `IHttpClientFactory` | Primary constructor | `System.Net.Http.IHttpClientFactory` |
| `IOptions<WebhookOptions>` | Primary constructor | Options pattern |
| `ILogger<HttpJobCompletionNotifier>` | Primary constructor | Logger |

### Branch Analysis

| Line(s) | Branch | Description |
|---------|--------|-------------|
| 36 | `string.IsNullOrEmpty(webhookUrl)` | No URL configured |
| 39 | Early return | Logs "NoWebhookUrlConfigured" and returns |
| 46-50 | try: success | POST succeeds, EnsureSuccessStatusCode passes |
| 54-57 | catch Exception | POST fails or status code error |

### Test Cases

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `NotifyReturnsEarlyWhenCompletionUrlIsEmpty` | Empty URL | WebhookOptions.CompletionUrl="" | NoWebhookUrlConfigured logged, no HTTP call made |
| `NotifyReturnsEarlyWhenCompletionUrlIsNull` | Null URL | WebhookOptions.CompletionUrl=null | NoWebhookUrlConfigured logged, no HTTP call made |
| `NotifySendsPostRequestToConfiguredUrl` | Happy path | Valid URL, mock HttpMessageHandler returns 200 | PostAsJsonAsync called with correct URL and payload |
| `NotifyLogsSuccessWhenWebhookSent` | Success logging | Mock returns 200 | CompletionWebhookSent logged |
| `NotifyLogsFailureWhenHttpCallThrows` | Exception | Mock throws HttpRequestException | CompletionWebhookFailed logged with exception message |
| `NotifyLogsFailureWhenStatusCodeIsError` | Non-success status | Mock returns 500 | CompletionWebhookFailed logged (EnsureSuccessStatusCode throws) |
| `NotifySetsTimeoutFromOptions` | Timeout config | WebhookOptions.TimeoutSeconds=15 | client.Timeout = 15 seconds |
| `NotifyUsesWebhookClientNamedClient` | Named client | Any | httpClientFactory.CreateClient("WebhookClient") called |
| `NotifySendsPayloadAsJson` | Serialization | Payload with all fields populated | Request body contains correct JSON |
| `NotifyDoesNotThrowOnException` | Exception swallowed | Any exception | Method completes without throwing |
| `NotifyPassesCancellationTokenToPostAsync` | Token forwarding | Use TestContext token | PostAsJsonAsync receives the token |

---

## NflApiClient

**File:** `Pipelines/NflApiClient.cs:17-53`
**Cyclomatic Complexity:** 4 (if !success, if result != null, else null, catch)

### Dependencies to Mock

| Dependency | Type | Notes |
|------------|------|-------|
| `HttpClient` | Constructor | Use MockHttpMessageHandler |
| `ILogger` | Constructor | Mock logger |

### Branch Analysis

| Line(s) | Branch | Description |
|---------|--------|-------------|
| 35 | `!response.IsSuccessStatusCode` | HTTP error response |
| 42-44 | `result != null` (ternary) | Deserialization returns non-null |
| 44 | `result == null` (ternary else) | Deserialization returns null |
| 48-51 | `catch (Exception ex)` | Network error / serialization error |

### Test Cases

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `GetReturnsSuccessWithDeserializedResponse` | Happy path | Mock returns 200 with valid JSON | Success with NflApiResponse<T> |
| `GetReturnsFailureWhenStatusCodeIsError` | HTTP error | Mock returns 404 | Failure, ApiCallFailed logged |
| `GetReturnsFailureWhenResponseIsNull` | Null deserialization | Mock returns 200, empty body | Failure, ApiResponseEmpty logged |
| `GetReturnsFailureWhenExceptionThrown` | Network failure | Mock throws HttpRequestException | Failure, ApiCallException logged |
| `GetReturnsFailureWhenDeserializationFails` | Bad JSON | Mock returns 200 with invalid JSON | Failure, ApiCallException logged (JsonException) |
| `GetSendsGetRequestToCorrectEndpoint` | Endpoint routing | endpoint="/nfl/stats" | HttpClient.GetAsync called with "/nfl/stats" |
| `GetPassesCancellationToken` | Token forwarding | Use TestContext token | GetAsync receives the token |

---

## DemoPipeline

**File:** `Pipelines/DemoPipeline.cs:69-166`
**Cyclomatic Complexity:** 8 (foreach, 2 if filters, switch with 5 cases, 2 catch blocks)

### Dependencies to Mock

| Dependency | Type | Notes |
|------------|------|-------|
| `ILogger` | Constructor | Mock logger |
| `DemoPipelineConfiguration` | Constructor | Config object (no mock needed, instantiate directly) |

Note: `DemoDataProvider.GetSamplePlayers()` is static with hardcoded data -- not mockable but deterministic.

### Branch Analysis

| Line(s) | Branch | Description |
|---------|--------|-------------|
| 100-104 | `input.Games < _config.MinGames` | Filter by minimum games |
| 107-112 | `!string.IsNullOrEmpty(FilterPosition) && !input.Position.Equals(...)` | Position filter |
| 118-125 | switch (totalYards) | 5 classifications: Elite(>=2000), ProBowl(>=1500), Starter(>=1000), Backup(>=500), Rotational(<500) |
| 155-158 | `catch (OperationCanceledException)` | Cancellation |
| 160-163 | `catch (Exception ex)` | Unexpected error |

### Test Cases

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `ExecuteReturnsSuccessWithAllRecordsWhenNoFilters` | Default config, MinGames=1 | Config with MinGames=1, no position filter | All 12 records extracted, none filtered |
| `ExecuteFiltersPlayersBelowMinGames` | MinGames filter | Config with MinGames=12 (default) | Tua Tagovailoa filtered (10 games), RecordsFailed=1 |
| `ExecuteFiltersPlayersByPosition` | Position filter | Config with FilterPosition="QB" | Only QBs remain in output |
| `ExecuteAppliesBothFiltersSimultaneously` | Combined filters | FilterPosition="QB", MinGames=15 | Only QBs with 15+ games |
| `ExecuteClassifiesEliteCorrectly` | Classification >= 2000 | Default config, check Mahomes (5228 yards) | Classification="Elite" |
| `ExecuteClassifiesProBowlCorrectly` | Classification 1500-1999 | Check Ja'Marr Chase (1783 yards) | Classification="Pro Bowl" |
| `ExecuteClassifiesStarterCorrectly` | Classification 1000-1499 | Check CeeDee Lamb (1278 yards) | Classification="Starter" |
| `ExecuteClassifiesBackupCorrectly` | Classification 500-999 | Check Travis Kelce (823 yards) | Classification="Backup" |
| `ExecuteClassifiesRotationalCorrectly` | Classification < 500 | Player with < 500 total yards | Classification="Rotational" |
| `ExecuteCalculatesTotalYardsCorrectly` | Yard calculation | Check a known player | TotalYards = PassingYards + RushingYards + ReceivingYards |
| `ExecuteHandlesNullYardValues` | Null nullable ints | Player with null PassingYards | Null treated as 0 |
| `ExecuteReturnsCorrectRecordCounts` | Count accuracy | Default config | RecordsExtracted=12, RecordsTransformed+RecordsFailed=12 |
| `ExecuteReturnsFailureOnCancellation` | OperationCanceledException | Cancel token before/during execution | Failure result |
| `ExecuteReturnsFailureOnUnexpectedException` | General exception | (difficult to trigger with static data -- may need reflection or config manipulation) | Failure result |
| `ExecuteReturnsDuration` | Timing | Normal execution | Duration > TimeSpan.Zero |
| `ExecuteReturnsTransformedRecordsInResult` | Output records | Default config | TransformedRecords populated with correct data |
| `ExecuteHandlesEmptyPositionFilterAsNoFilter` | Empty string filter | FilterPosition="" | No position filtering applied |
| `ExecutePositionFilterIsCaseInsensitive` | Case insensitivity | FilterPosition="qb" (lowercase) | Matches "QB" positions |

---

## NflApiPipeline

**File:** `Pipelines/NflApiPipeline.cs:59-192`
**Cyclomatic Complexity:** 10 (if Season, if Position, if !extractResult, foreach, if MinGames, switch 6 cases, if Games>0, 2 catch)

### Dependencies to Mock

| Dependency | Type | Notes |
|------------|------|-------|
| `ILogger` | Constructor | Mock logger |
| `NflApiPipelineConfiguration` | Constructor | Config object (instantiate directly) |
| `IHttpClientFactory` | Constructor | Mock to control NflApiClient behavior |

### Branch Analysis

| Line(s) | Branch | Description |
|---------|--------|-------------|
| 92-93 | `_config.Season.HasValue` | Append season to query |
| 94-95 | `!string.IsNullOrEmpty(_config.Position)` | Append position to query |
| 101 | `!extractResult.IsSuccess \|\| extractResult.Value == null` | Extract failed |
| 121-125 | `player.Games < _config.MinGames` | Filter by min games |
| 132 | `player.Games > 0` ternary | Avoid divide-by-zero |
| 135-143 | switch (totalYards) | 6 classifications: MVP(>=4000), Elite(>=2000), ProBowl(>=1500), Starter(>=1000), Backup(>=500), Rotational |
| 181-184 | `catch (OperationCanceledException)` | Pipeline cancelled |
| 186-189 | `catch (Exception ex)` | Unexpected error |

### Test Cases

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `ExecuteReturnsSuccessWithTransformedData` | Happy path | Mock API returns valid data | Success with all metrics |
| `ExecuteBuildsQueryWithSeasonWhenProvided` | Season param | Config with Season=2024 | Query contains "&season=2024" |
| `ExecuteBuildsQueryWithoutSeasonWhenNull` | No season | Config with Season=null | Query does not contain "season" |
| `ExecuteBuildsQueryWithPositionWhenProvided` | Position param | Config with Position="QB" | Query contains "&position=QB" |
| `ExecuteBuildsQueryWithoutPositionWhenEmpty` | No position | Config with Position="" or null | Query does not contain "position" |
| `ExecuteBuildsQueryWithBothSeasonAndPosition` | Both params | Both set | Query contains both parameters |
| `ExecuteReturnsFailureWhenExtractFails` | API failure | Mock API returns failure | Failure, ExtractFailed logged |
| `ExecuteReturnsFailureWhenExtractReturnsNull` | Null extract | Mock returns Success(null) | Failure, ExtractFailed logged |
| `ExecuteFiltersPlayersBelowMinGames` | MinGames filter | Player with Games < MinGames | Player excluded, RecordsFailed incremented |
| `ExecuteCalculatesTotalYardsCorrectly` | Yard sum | Known player data | TotalYards = Passing + Rushing + Receiving |
| `ExecuteCalculatesTotalTouchdownsCorrectly` | TD sum | Known player data | TotalTouchdowns = PassingTDs + RushingTDs + ReceivingTDs |
| `ExecuteCalculatesEfficiencyRatingCorrectly` | Yards/game | Player with 1700 yards in 17 games | EfficiencyRating = Math.Round(100.0, 1) |
| `ExecuteHandlesZeroGamesWithoutDivideByZero` | Zero games edge | Player with Games=0 | EfficiencyRating = 0, no exception |
| `ExecuteClassifiesMvpCandidateCorrectly` | >= 4000 yards | Player with 4500 yards | Classification="MVP Candidate" |
| `ExecuteClassifiesEliteCorrectly` | 2000-3999 yards | Player with 2500 yards | Classification="Elite" |
| `ExecuteClassifiesProBowlCorrectly` | 1500-1999 | Player with 1700 yards | Classification="Pro Bowl" |
| `ExecuteClassifiesStarterCorrectly` | 1000-1499 | Player with 1200 yards | Classification="Starter" |
| `ExecuteClassifiesBackupCorrectly` | 500-999 | Player with 700 yards | Classification="Backup" |
| `ExecuteClassifiesRotationalCorrectly` | < 500 | Player with 300 yards | Classification="Rotational" |
| `ExecuteHandlesNullStatValues` | Null nullable ints | Player with null PassingYards etc. | Null treated as 0 |
| `ExecuteRoundsEfficiencyToOneDecimal` | Rounding | Player with non-round efficiency | Math.Round(x, 1) applied |
| `ExecuteReturnsFailureOnCancellation` | OperationCanceledException | Cancel during execution | Failure, PipelineCancelled logged |
| `ExecuteReturnsFailureOnUnexpectedException` | General exception | Mock throws unexpected | Failure, PipelineFailed logged |
| `ExecuteReturnsCorrectRecordCounts` | Metrics | Multiple players, some filtered | RecordsExtracted, RecordsFailed, RecordsLoaded all correct |
| `ExecuteSetsPageSizeInQuery` | PageSize param | Config with PageSize=50 | Query contains "pageSize=50" |
| `ExecuteUsesDefaultPageSize` | Default PageSize | Default config | Query contains "pageSize=100" |

---

## JobTriggerSources TypeCollection

**File:** `Services/JobTriggerSources/` (multiple files)
**Cyclomatic Complexity:** 1 per TypeOption (trivial constructors)

### Components

| File | Class | Complexity | Testable? |
|------|-------|-----------|-----------|
| `IJobTriggerSource.cs` | `IJobTriggerSource` | N/A (interface) | No |
| `JobTriggerSourceBase.cs` | `JobTriggerSourceBase` | 1 | Exclude - abstract base, trivial |
| `JobTriggerSources.cs` | `JobTriggerSourceTypes` | 1 | TypeCollection lookup tests |
| `Options/ApiJobTriggerSource.cs` | `ApiJobTriggerSource` | 1 | Via collection |
| `Options/EventJobTriggerSource.cs` | `EventJobTriggerSource` | 1 | Via collection |
| `Options/ManualJobTriggerSource.cs` | `ManualJobTriggerSource` | 1 | Via collection |
| `Options/ScheduledJobTriggerSource.cs` | `ScheduledJobTriggerSource` | 1 | Via collection |

### Test Cases

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `AllReturnsAllFourTriggerSources` | Collection completeness | Call All() | 4 items |
| `ByNameReturnsApiTriggerSource` | Name lookup | ByName("Api") | ApiJobTriggerSource with Id=4 |
| `ByNameReturnsEventTriggerSource` | Name lookup | ByName("Event") | EventJobTriggerSource with Id=3 |
| `ByNameReturnsManualTriggerSource` | Name lookup | ByName("Manual") | ManualJobTriggerSource with Id=1 |
| `ByNameReturnsScheduledTriggerSource` | Name lookup | ByName("Scheduled") | ScheduledJobTriggerSource with Id=2 |
| `ByIdReturnsCorrectType` | Id lookup | ById(1) | ManualJobTriggerSource |
| `ByNameReturnsFailureForUnknownName` | Unknown name | ByName("Unknown") | Failure result |

Note: These tests require a `TypeCollectionFixture` that calls `JobTriggerSourceTypes.All()` in its constructor to trigger source-generated registration. The test class needs the `[Collection]` attribute pointing to this fixture. See MEMORY.md RestrictToCurrentCompilation pattern.

---

## Trivial/Excluded Types

These types have complexity 1 and should be EXCLUDED from code coverage:

| Type | File | Reason |
|------|------|--------|
| `TriggerJobRequest` | `Endpoints/TriggerJobEndpoint.cs:9-14` | POCO with auto-properties |
| `TriggerJobResponse` | `Endpoints/TriggerJobEndpoint.cs:16-20` | POCO with auto-properties |
| `JobStatusRequest` | `Endpoints/GetJobStatusEndpoint.cs:9-12` | POCO with single property |
| `JobStatusResponse` | `Services/IJobExecutionService.cs:51-61` | Record type, no logic |
| `JobCompletionPayload` | `Services/HttpJobCompletionNotifier.cs:18-25` | Record type, no logic |
| `ExecutionUpdateRecord` | `Services/ExecutionUpdateRecord.cs:9-19` | POCO with auto-properties |
| `WebhookOptions` | `Configuration/WebhookOptions.cs:3-8` | Config class, no logic |
| `IJobTriggerSource` | `Services/JobTriggerSources/IJobTriggerSource.cs:5-8` | Marker interface |
| `JobTriggerSourceBase` | `Services/JobTriggerSources/JobTriggerSourceBase.cs:5-8` | Abstract base, passthrough constructor |
| `DemoPlayerInput` | `Pipelines/DemoDataProvider.cs:8-20` | POCO |
| `DemoPlayerOutput` | `Pipelines/DemoPipeline.cs:41-50` | POCO |
| `DemoPipelineConfiguration` | `Pipelines/DemoPipeline.cs:15-36` | Config POCO |
| `DemoPipelineResult` | `Pipelines/DemoPipeline.cs:55-64` | POCO |
| `NflPlayerStatDto` | `Pipelines/NflPlayerStatDto.cs:7-24` | DTO |
| `NflApiPlayerOutput` | `Pipelines/NflApiPipeline.cs:29-40` | POCO |
| `NflApiPipelineConfiguration` | `Pipelines/NflApiPipeline.cs:17-24` | Config POCO |
| `NflApiPipelineResult` | `Pipelines/NflApiPipeline.cs:45-54` | POCO |
| `NflApiResponse<T>` | `Pipelines/NflApiClient.cs:58-64` | POCO |
| `ProgramLog` | `Logging/ProgramLog.cs` | Source-generated logging, tested via generator tests |
| `EtlServerLog` | `Logging/EtlServerLog.cs` | Source-generated logging, tested via generator tests |
| `WebhookLog` | `Logging/WebhookLog.cs` | Source-generated logging, tested via generator tests |
| `IJobExecutionService` | `Services/IJobExecutionService.cs:11-36` | Interface, no logic |
| `DemoDataProvider` | `Pipelines/DemoDataProvider.cs:25-53` | Static data factory, complexity 1 |
| `Program` | `Program.cs` | Startup orchestration, tested via integration |

---

## Summary

### Test File Structure

```
tests/Reference.Etl.Server.Tests/
  Endpoints/
    TriggerJobEndpointTests.cs      (8 tests)
    GetJobStatusEndpointTests.cs    (5 tests)
  Services/
    DefaultJobExecutionServiceTests.cs
      TriggerJobTests.cs            (14 tests)
      ExecutePipelineTests.cs       (8 tests -- indirect via TriggerJob)
      GetJobStatusTests.cs          (15 tests)
    HttpJobCompletionNotifierTests.cs (11 tests)
  Pipelines/
    NflApiClientTests.cs            (7 tests)
    DemoPipelineTests.cs            (18 tests)
    NflApiPipelineTests.cs          (26 tests)
  TypeCollections/
    JobTriggerSourceTypesTests.cs   (7 tests)
```

### Total Test Count

| Class | Test Count |
|-------|-----------|
| TriggerJobEndpoint | 8 |
| GetJobStatusEndpoint | 5 |
| DefaultJobExecutionService.TriggerJob | 14 |
| DefaultJobExecutionService.ExecutePipeline | 8 |
| DefaultJobExecutionService.CompleteWithMetrics | 4 |
| DefaultJobExecutionService.GetJobStatus | 15 |
| HttpJobCompletionNotifier | 11 |
| NflApiClient | 7 |
| DemoPipeline | 18 |
| NflApiPipeline | 26 |
| JobTriggerSourceTypes | 7 |
| **TOTAL** | **123** |

### Complexity Summary

| Class | Method | Complexity | Status |
|-------|--------|-----------|--------|
| TriggerJobEndpoint | HandleAsync | 3 | OK |
| TriggerJobEndpoint | Configure | 1 | Trivial |
| GetJobStatusEndpoint | HandleAsync | 3 | OK |
| GetJobStatusEndpoint | Configure | 1 | Trivial |
| DefaultJobExecutionService | TriggerJob | 6 | OK |
| DefaultJobExecutionService | ExecutePipeline | 9 | OK |
| DefaultJobExecutionService | CompleteWithMetrics | 2 | OK |
| DefaultJobExecutionService | GetJobStatus | 5 | OK |
| HttpJobCompletionNotifier | Notify | 4 | OK |
| NflApiClient | Get<T> | 4 | OK |
| DemoPipeline | Execute | 8 | OK |
| NflApiPipeline | Execute | 10 | OK |

No methods exceed complexity 10 (threshold). NflApiPipeline.Execute at 10 is at the boundary but acceptable given the sequential ETL structure.

### Key Testing Challenges

1. **DefaultJobExecutionService.ExecutePipeline is private** -- Must be tested indirectly through TriggerJob. The `Task.Run` fire-and-forget pattern makes it difficult to await completion. Tests will need a mechanism to detect when the background task finishes (e.g., polling the mock ExecutionTracker.Complete call, or using TaskCompletionSource in the mock).

2. **FastEndpoints testing** -- TriggerJobEndpoint and GetJobStatusEndpoint extend `Endpoint<TReq, TResp>`. Tests can either use FastEndpoints' built-in test infrastructure (`Factory.Create<TEndpoint>()`) or test the service layer directly (preferred for unit tests).

3. **HttpClient mocking** -- NflApiClient and HttpJobCompletionNotifier both use HttpClient. Tests should use a custom `DelegatingHandler` / `MockHttpMessageHandler` to intercept HTTP calls.

4. **TypeCollection registration** -- JobTriggerSourceTypes tests require the source-generated module initializer to run. Use a shared `TypeCollectionFixture` with `[Collection]` attribute.

5. **DemoPipeline uses static DemoDataProvider** -- Data is deterministic (12 hardcoded players), so tests can assert against known values. Not mockable without abstraction, but not needed since data is fixed.
