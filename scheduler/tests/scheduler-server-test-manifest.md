# SchedulerServer Test Requirements Manifest

## Overview

This manifest covers all testable classes in `Reference.Scheduler.Server`.
Test project: `Reference.Scheduler.Server.Tests` (xUnit v3, Shouldly, Moq).

**NOTE:** EtlDispatchService tests assume the REFACTORED version that injects
`IPipelineJobClient` instead of `IHttpClientFactory`. The retry loop and backoff
logic remain the same; only the inner dispatch mechanism changes.

---

## 1. CronJobScheduler

**File:** `/home/mike/projects/FractalDataWorks/public/ReferenceSolutions/SchedulerServer/src/Reference.Scheduler.Server/Services/JobSchedulers/Options/CronJobScheduler.cs`

### Complexity Metrics

| Method | Complexity | Status |
|--------|------------|--------|
| `IsDue` | 4 | OK |
| `GetNextRunTime` | 3 | OK |

### Dependencies to Mock

None. Pure logic class with no dependencies.

### Test Cases

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `IsDueReturnsFalseWhenExpressionIsNull` | Null expression guard | `expression = null` | `false` |
| `IsDueReturnsFalseWhenExpressionIsEmpty` | Empty expression guard | `expression = ""` | `false` |
| `IsDueReturnsFalseWhenExpressionIsWhitespace` | Whitespace expression guard | `expression = "  "` | `false` |
| `IsDueReturnsTrueWhenNextOccurrenceIsBeforeNow` | Happy path - due | `lastRun = 1hr ago`, `now = UtcNow`, `expression = "* * * * *"` (every minute) | `true` |
| `IsDueReturnsFalseWhenNextOccurrenceIsAfterNow` | Not yet due | `lastRun = now - 1s`, `now = UtcNow`, `expression = "0 0 1 1 *"` (yearly) | `false` |
| `IsDueReturnsTrueWhenLastRunIsNull` | First run ever | `lastRun = null`, `now = UtcNow`, `expression = "* * * * *"` | `true` (next after MinValue <= now) |
| `IsDueReturnsFalseWhenExpressionIsInvalid` | Invalid cron catch | `expression = "invalid cron"` | `false` |
| `GetNextRunTimeReturnsNullWhenExpressionIsNull` | Null expression guard | `expression = null` | `null` |
| `GetNextRunTimeReturnsNullWhenExpressionIsEmpty` | Empty expression guard | `expression = ""` | `null` |
| `GetNextRunTimeReturnsNullWhenExpressionIsInvalid` | Invalid cron catch | `expression = "not valid"` | `null` |
| `GetNextRunTimeReturnsExpectedTimeForValidCron` | Happy path | `from = 2026-01-01 00:00`, `expression = "0 12 * * *"` | `2026-01-01 12:00 UTC` |
| `GetNextRunTimeReturnsNullWhenExpressionIsWhitespace` | Whitespace guard | `expression = "   "` | `null` |

---

## 2. IntervalJobScheduler

**File:** `/home/mike/projects/FractalDataWorks/public/ReferenceSolutions/SchedulerServer/src/Reference.Scheduler.Server/Services/JobSchedulers/Options/IntervalJobScheduler.cs`

### Complexity Metrics

| Method | Complexity | Status |
|--------|------------|--------|
| `IsDue` | 5 | OK |
| `GetNextRunTime` | 3 | OK |

### Dependencies to Mock

None. Pure logic class.

### Test Cases

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `IsDueReturnsFalseWhenExpressionIsNull` | Null guard | `expression = null` | `false` |
| `IsDueReturnsFalseWhenExpressionIsEmpty` | Empty guard | `expression = ""` | `false` |
| `IsDueReturnsFalseWhenExpressionIsNotNumeric` | Parse failure | `expression = "abc"` | `false` |
| `IsDueReturnsFalseWhenIntervalIsZero` | Zero interval guard | `expression = "0"` | `false` |
| `IsDueReturnsFalseWhenIntervalIsNegative` | Negative interval guard | `expression = "-5"` | `false` |
| `IsDueReturnsTrueWhenLastRunIsNull` | First run | `lastRun = null` | `true` |
| `IsDueReturnsTrueWhenElapsedExceedsInterval` | Interval elapsed | `lastRun = now - 120s`, `expression = "60"` | `true` |
| `IsDueReturnsFalseWhenElapsedLessThanInterval` | Interval not elapsed | `lastRun = now - 30s`, `expression = "60"` | `false` |
| `IsDueReturnsTrueWhenElapsedEqualsInterval` | Exact boundary | `lastRun = now - 60s`, `expression = "60"` | `true` |
| `GetNextRunTimeReturnsNullWhenExpressionIsNull` | Null guard | `expression = null` | `null` |
| `GetNextRunTimeReturnsNullWhenExpressionIsNotNumeric` | Parse failure | `expression = "abc"` | `null` |
| `GetNextRunTimeReturnsNullWhenIntervalIsZero` | Zero interval | `expression = "0"` | `null` |
| `GetNextRunTimeReturnsCorrectTimeForValidInterval` | Happy path | `from = 2026-01-01 00:00`, `expression = "3600"` | `2026-01-01 01:00` |

---

## 3. ManualJobScheduler

**File:** `/home/mike/projects/FractalDataWorks/public/ReferenceSolutions/SchedulerServer/src/Reference.Scheduler.Server/Services/JobSchedulers/Options/ManualJobScheduler.cs`

### Complexity Metrics

| Method | Complexity | Status |
|--------|------------|--------|
| `IsDue` | 1 | Trivial |
| `GetNextRunTime` | 1 | Trivial |

### Dependencies to Mock

None.

### Test Cases

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `IsDueAlwaysReturnsFalse` | Only behavior | Any inputs | `false` |
| `GetNextRunTimeAlwaysReturnsNull` | Only behavior | Any inputs | `null` |

---

## 4. OneTimeJobScheduler

**File:** `/home/mike/projects/FractalDataWorks/public/ReferenceSolutions/SchedulerServer/src/Reference.Scheduler.Server/Services/JobSchedulers/Options/OneTimeJobScheduler.cs`

### Complexity Metrics

| Method | Complexity | Status |
|--------|------------|--------|
| `IsDue` | 4 | OK |
| `GetNextRunTime` | 1 | Trivial |

### Dependencies to Mock

None. Pure logic class.

### Test Cases

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `IsDueReturnsFalseWhenExpressionIsNull` | Null guard | `expression = null` | `false` |
| `IsDueReturnsFalseWhenExpressionIsEmpty` | Empty guard | `expression = ""` | `false` |
| `IsDueReturnsFalseWhenExpressionIsNotValidDateTime` | Parse failure | `expression = "not a date"` | `false` |
| `IsDueReturnsTrueWhenScheduledTimeIsInPastAndNeverRun` | Happy path - due | `scheduledTime = now - 1hr`, `lastRun = null` | `true` |
| `IsDueReturnsTrueWhenScheduledTimeEqualsNowAndNeverRun` | Exact boundary | `scheduledTime = now`, `lastRun = null` | `true` |
| `IsDueReturnsFalseWhenScheduledTimeIsInFuture` | Not yet due | `scheduledTime = now + 1hr`, `lastRun = null` | `false` |
| `IsDueReturnsFalseWhenAlreadyRun` | Already executed once | `scheduledTime = now - 1hr`, `lastRun = now - 30min` | `false` |
| `GetNextRunTimeAlwaysReturnsNull` | Only behavior | Any inputs | `null` |

---

## 5. DefaultSchedulerService

**File:** `/home/mike/projects/FractalDataWorks/public/ReferenceSolutions/SchedulerServer/src/Reference.Scheduler.Server/Services/DefaultSchedulerService.cs`

### Complexity Metrics

| Method | Complexity | Status |
|--------|------------|--------|
| `CreateSchedule` | 3 (try/catch, success check) | OK |
| `DeleteSchedule` | 4 (try/catch, success check, zero-rows check) | OK |
| `GetSchedules` | 4 (try/catch, success check, tenant filter) | OK |
| `GetSchedule` | 5 (try/catch, success check, null record, tenant filter) | OK |
| `UpdateSchedule` | 4 (try/catch, success check, zero-rows check) | OK |
| Constructor | 2 (null config check) | OK |

### Dependencies to Mock

- `ILogger<DefaultSchedulerService>` (Mock)
- `IOptionsMonitor<List<CronScheduleConfiguration>>` (Mock)
- `IOptionsMonitor<List<IntervalScheduleConfiguration>>` (Mock)
- `IDataGateway` (Mock)
- `IOptionsMonitor<List<FractalDataWorks.Services.Scheduling.SchedulerConfiguration>>` (Mock)
- `ITenantContext?` (Mock, optional)

### Test Cases

#### Constructor

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `ConstructorThrowsWhenSchedulerConfigurationIsEmpty` | Config list empty | `fdwOptions.CurrentValue = []` | `InvalidOperationException` |
| `ConstructorThrowsWhenSchedulerConfigurationIsNull` | Config null | `fdwOptions.CurrentValue = null` | `InvalidOperationException` or `NullReferenceException` |
| `ConstructorSucceedsWithValidConfiguration` | Happy path | Valid config list with one entry | No exception |

#### CreateSchedule

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `CreateScheduleReturnsSuccessWithValidInput` | Happy path | Gateway returns `Success(1)` | `IsSuccess = true`, `Value = scheduleName` |
| `CreateScheduleReturnsFailureWhenGatewayFails` | Gateway failure | Gateway returns `Failure(...)` | `IsSuccess = false` |
| `CreateScheduleReturnsFailureWhenExceptionThrown` | Exception path | Gateway throws `Exception` | `IsSuccess = false` |
| `CreateScheduleSetsTenantIdFromContext` | Tenant context present | `tenantContext.TenantId = Guid` | Verify insert record has TenantId set |

#### DeleteSchedule

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `DeleteScheduleReturnsSuccessWhenRowDeleted` | Happy path | Gateway returns `Success(1)` | `IsSuccess = true` |
| `DeleteScheduleReturnsFailureWhenGatewayFails` | Gateway failure | Gateway returns `Failure(...)` | `IsSuccess = false` |
| `DeleteScheduleReturnsFailureWhenNoRowsAffected` | Not found (0 rows) | Gateway returns `Success(0)` | `IsSuccess = false` |
| `DeleteScheduleReturnsFailureWhenExceptionThrown` | Exception path | Gateway throws `Exception` | `IsSuccess = false` |

#### GetSchedules

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `GetSchedulesReturnsSuccessWithScheduleList` | Happy path | Gateway returns list of records | `IsSuccess = true`, mapped ScheduleInfo list |
| `GetSchedulesReturnsSuccessWithEmptyList` | No schedules | Gateway returns `Success(empty)` | `IsSuccess = true`, empty list |
| `GetSchedulesReturnsFailureWhenGatewayFails` | Gateway failure | Gateway returns `Failure(...)` | `IsSuccess = false` |
| `GetSchedulesReturnsFailureWhenExceptionThrown` | Exception path | Gateway throws | `IsSuccess = false` |
| `GetSchedulesFiltersByTenantWhenTenantContextPresent` | Tenant filtering | `tenantContext.HasTenant = true`, `TenantId = Guid` | Verify query includes tenant filter |
| `GetSchedulesDoesNotFilterWhenNoTenantContext` | No tenant | `tenantContext = null` | Verify query has no tenant filter |

#### GetSchedule

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `GetScheduleReturnsSuccessWhenFound` | Happy path | Gateway returns list with one record | `IsSuccess = true`, correct ScheduleInfo |
| `GetScheduleReturnsFailureWhenNotFound` | Not found | Gateway returns empty list | `IsSuccess = false` |
| `GetScheduleReturnsFailureWhenGatewayFails` | Gateway failure | Gateway returns `Failure(...)` | `IsSuccess = false` |
| `GetScheduleReturnsFailureWhenExceptionThrown` | Exception path | Gateway throws | `IsSuccess = false` |
| `GetScheduleFiltersByTenantWhenTenantContextPresent` | Tenant filtering | `tenantContext.HasTenant = true` | Verify query includes tenant filter |

#### UpdateSchedule

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `UpdateScheduleReturnsSuccessWhenRowUpdated` | Happy path | Gateway returns `Success(1)` | `IsSuccess = true` |
| `UpdateScheduleReturnsFailureWhenGatewayFails` | Gateway failure | Gateway returns `Failure(...)` | `IsSuccess = false` |
| `UpdateScheduleReturnsFailureWhenNoRowsAffected` | Not found (0 rows) | Gateway returns `Success(0)` | `IsSuccess = false` |
| `UpdateScheduleReturnsFailureWhenExceptionThrown` | Exception path | Gateway throws | `IsSuccess = false` |

---

## 6. EtlDispatchService (REFACTORED - IPipelineJobClient)

**File:** `/home/mike/projects/FractalDataWorks/public/ReferenceSolutions/SchedulerServer/src/Reference.Scheduler.Server/Services/EtlDispatchService.cs`

**Post-refactoring constructor:**
```csharp
public EtlDispatchService(
    IPipelineJobClient pipelineJobClient,
    ILogger<EtlDispatchService> logger,
    IOptions<EtlDispatchConfiguration> options)
```

### Complexity Metrics

| Method | Complexity | Status |
|--------|------------|--------|
| `Dispatch` | 8 (retry loop, success/failure per attempt, catch blocks) | OK |
| `DispatchOnce` (refactored) | 2 (success/failure from client) | OK |

### Dependencies to Mock

- `IPipelineJobClient` (Mock)
- `ILogger<EtlDispatchService>` (Mock)
- `IOptions<EtlDispatchConfiguration>` (Mock)

### Test Cases

#### Dispatch - Success Paths

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `DispatchReturnsSuccessOnFirstAttempt` | Happy path, no retry | Client returns success on first call | `IsSuccess = true` |
| `DispatchReturnsSuccessAfterRetry` | Success on retry | Client fails 1st, succeeds 2nd | `IsSuccess = true`, verify DispatchRetrySucceeded logged |

#### Dispatch - Failure Paths (Result-Based)

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `DispatchReturnsFailureAfterAllRetriesExhaustedWithFailureResult` | All retries fail (result) | Client returns failure on all attempts | `IsSuccess = false` |
| `DispatchRetriesToMaxAttemptsOnFailureResult` | Retry count verification | MaxRetries = 2, client always fails | Client called 3 times (1 + 2 retries) |

#### Dispatch - Failure Paths (Exception-Based)

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `DispatchReturnsFailureAfterAllRetriesExhaustedWithException` | All retries fail (exception) | Client throws on all attempts | `IsSuccess = false` |
| `DispatchRetriesToMaxAttemptsOnException` | Retry count on exception | MaxRetries = 2, client always throws | Client called 3 times |

#### Dispatch - Cancellation

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `DispatchThrowsOperationCanceledWhenCancelled` | Cancellation during dispatch | Token cancelled after first attempt | `OperationCanceledException` thrown (not caught) |
| `DispatchThrowsOperationCanceledDuringDelay` | Cancellation during retry delay | Token cancelled during Task.Delay | `OperationCanceledException` thrown |

#### Dispatch - Exponential Backoff

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `DispatchUsesExponentialBackoffBetweenRetries` | Verify delay doubling | MaxRetries = 3, RetryDelaySeconds = 2, all fail | Delays: 2s, 4s, 8s (approximate, verify via timing or mock) |

#### Dispatch - Configuration

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `DispatchRespectsMaxRetriesConfiguration` | MaxRetries = 0 | Config has MaxRetries = 0 | Client called exactly once |
| `DispatchUsesConfiguredRetryDelay` | Custom delay | RetryDelaySeconds = 5 | First retry delay is 5s |

---

## 7. SchedulerBackgroundService

**File:** `/home/mike/projects/FractalDataWorks/public/ReferenceSolutions/SchedulerServer/src/Reference.Scheduler.Server/Services/SchedulerBackgroundService.cs`

### Complexity Metrics

| Method | Complexity | Status |
|--------|------------|--------|
| `ExecuteAsync` | 5 (loop, two try/catch, cancellation checks) | OK |
| `EvaluateSchedules` | 8 (scope, null checks, foreach, conditional dispatch, update) | OK |

### Dependencies to Mock

- `IServiceScopeFactory` (Mock)
- `IServiceScope` (Mock, returned by factory)
- `IServiceProvider` (Mock, from scope)
- `ISchedulerService` (Mock, resolved from provider)
- `IEtlDispatchService` (Mock, resolved from provider)
- `ILogger<SchedulerBackgroundService>` (Mock)
- `IOptions<SchedulerConfiguration>` (Mock)

### Test Cases

#### ExecuteAsync

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `ExecuteAsyncCallsEvaluateSchedulesAndLoops` | Basic loop | Cancel after first evaluation | `EvaluateSchedules` called at least once |
| `ExecuteAsyncStopsWhenCancellationRequested` | Graceful shutdown | Cancel token immediately | Loop exits, SchedulerStopped logged |
| `ExecuteAsyncContinuesAfterEvaluationException` | Error resilience | `GetSchedules` throws on 1st call, succeeds on 2nd | Loop continues, EvaluationLoopError logged |
| `ExecuteAsyncLogsSchedulerStartedWithInterval` | Startup logging | `EvaluationIntervalSeconds = 30` | SchedulerStarted logged with interval 30 |

#### EvaluateSchedules

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `EvaluateSchedulesReturnsEarlyWhenGetSchedulesFails` | GetSchedules failure | `GetSchedules` returns failure | No dispatch calls |
| `EvaluateSchedulesReturnsEarlyWhenScheduleListIsNull` | Null schedules | `GetSchedules` returns success with null value | No dispatch calls |
| `EvaluateSchedulesSkipsDisabledSchedules` | Disabled schedule | Schedule with `IsEnabled = false` | No dispatch for that schedule |
| `EvaluateSchedulesSkipsUnknownSchedulerType` | Unknown type | Schedule with `ServiceOptionType = "Unknown"` | EvaluatorNotFound logged, no dispatch |
| `EvaluateSchedulesDispatchesDueSchedule` | Happy path - Cron due | Cron schedule that is due | Dispatch called with correct args |
| `EvaluateSchedulesUpdatesScheduleAfterSuccessfulDispatch` | Post-dispatch update | Dispatch returns success | UpdateSchedule called with new LastRunTime and NextRunTime |
| `EvaluateSchedulesDoesNotUpdateScheduleWhenDispatchFails` | Dispatch failure | Dispatch returns failure | UpdateSchedule NOT called |
| `EvaluateSchedulesLogsUpdateFailure` | Update fails after dispatch | Dispatch succeeds, UpdateSchedule fails | ScheduleUpdateFailed logged |
| `EvaluateSchedulesUsesCorrectExpressionForCronType` | Cron expression mapping | `ServiceOptionType = "Cron"`, `CronExpression = "0 * * * *"` | CronExpression passed to IsDue |
| `EvaluateSchedulesUsesCorrectExpressionForIntervalType` | Interval expression mapping | `ServiceOptionType = "Interval"`, `IntervalSeconds = 300` | `"300"` passed to IsDue |
| `EvaluateSchedulesUsesCorrectExpressionForOneTimeType` | OneTime expression mapping | `ServiceOptionType = "OneTime"`, `NextRunTime = someTime` | ISO 8601 string passed to IsDue |
| `EvaluateSchedulesPassesNullExpressionForManualType` | Manual type mapping | `ServiceOptionType = "Manual"` | `null` passed to IsDue (Manual always returns false) |
| `EvaluateSchedulesLogsNotDueForNonDueSchedule` | Not due logging | Schedule is not due | ScheduleNotDue logged |
| `EvaluateSchedulesCountsDispatchedJobs` | Job count tracking | 2 due schedules out of 3 | EvaluationCompleted logged with count=3, dispatched=2 |

---

## 8. PreComputeCalculationsJob

**File:** `/home/mike/projects/FractalDataWorks/public/ReferenceSolutions/SchedulerServer/src/Reference.Scheduler.Server/Services/PreComputeCalculationsJob.cs`

### Complexity Metrics

| Method | Complexity | Status |
|--------|------------|--------|
| `StartAsync` | 2 (enabled check) | OK |
| `Execute` | 1 | Trivial (fire-and-forget wrapper) |
| `ExecuteCore` | 3 (semaphore, try/catch) | OK |
| `ExecutePreCompute` | 1 | Trivial (placeholder) |
| `StopAsync` | 1 | Trivial |
| `Dispose` | 2 (disposed check) | OK |

### Dependencies to Mock

- `IOptions<PreComputeOptions>` (Mock)
- `ILogger<PreComputeCalculationsJob>` (Mock)

### Test Cases

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `StartAsyncCreatesTimerWhenEnabled` | Happy path | `Enabled = true` | Timer created (JobStarted logged) |
| `StartAsyncDoesNotCreateTimerWhenDisabled` | Disabled | `Enabled = false` | JobDisabled logged, no timer |
| `ExecuteCoreSkipsWhenPreviousRunStillExecuting` | Semaphore guard | Semaphore already held | JobSkippedStillRunning logged |
| `ExecuteCoreReleaseSemaphoreAfterException` | Exception cleanup | ExecutePreCompute throws | Semaphore released, JobFailedUnexpectedError logged |
| `ExecutePreComputeLogsPlaceholderExecution` | Placeholder behavior | Normal execution | JobExecutingPlaceholder + JobCompleted logged |
| `StopAsyncDisablesTimer` | Graceful stop | Timer running | Timer disabled, JobStopped logged |
| `DisposeDisposesTimerAndSemaphore` | Disposal | Job created | No exception on dispose |
| `DisposeIsIdempotent` | Double dispose | Call Dispose twice | No exception |

---

## 9. ScheduleConfigurationValidator

**File:** `/home/mike/projects/FractalDataWorks/public/ReferenceSolutions/SchedulerServer/src/Reference.Scheduler.Server/Validation/ScheduleConfigurationValidator.cs`

### Complexity Metrics

| Method | Complexity | Status |
|--------|------------|--------|
| Constructor (rules) | 6 (multiple When/RuleFor) | OK |
| `Validate(string?, ScheduleConfiguration)` | 2 (valid/invalid) | OK |
| `BeValidCronExpression` | 4 (null check, part count, loop) | OK |
| `IsValidCronPart` | 2 (loop with char check) | OK |

### Dependencies to Mock

None. Pure validation logic.

### Test Cases

#### Name Validation

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `ValidateFailsWhenNameIsEmpty` | Required field | `Name = ""` | Validation fails with "Name is required" |
| `ValidateFailsWhenNameExceeds200Characters` | Max length | `Name = new string('a', 201)` | Validation fails |
| `ValidateFailsWhenNameStartsWithNumber` | Regex pattern | `Name = "1abc"` | Validation fails with regex message |
| `ValidateFailsWhenNameContainsSpaces` | Regex pattern | `Name = "my schedule"` | Validation fails |
| `ValidateSucceedsWithValidName` | Happy path | `Name = "DailySync"` | Validation passes for Name |
| `ValidateSucceedsWithNameContainingHyphensAndUnderscores` | Valid special chars | `Name = "my-schedule_v2"` | Validation passes |

#### PipelineName Validation

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `ValidateFailsWhenPipelineNameIsEmpty` | Required field | `PipelineName = ""` | Validation fails |

#### ServiceOptionType Validation

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `ValidateFailsWhenServiceOptionTypeIsEmpty` | Required field | `ServiceOptionType = ""` | Validation fails |
| `ValidateFailsWhenServiceOptionTypeIsInvalid` | Invalid type | `ServiceOptionType = "Weekly"` | Validation fails |
| `ValidateSucceedsForCronType` | Valid type | `ServiceOptionType = "Cron"` with valid CronExpression | Passes |
| `ValidateSucceedsForIntervalType` | Valid type | `ServiceOptionType = "Interval"` with IntervalSeconds > 0 | Passes |
| `ValidateSucceedsForManualType` | Valid type | `ServiceOptionType = "Manual"` | Passes |
| `ValidateSucceedsForOneTimeType` | Valid type | `ServiceOptionType = "OneTime"` | Passes |

#### Conditional Cron Rules

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `ValidateFailsWhenCronTypeHasNoCronExpression` | Cron without expression | `ServiceOptionType = "Cron"`, `CronExpression = null` | Validation fails |
| `ValidateFailsWhenCronExpressionHasWrongPartCount` | Bad cron format | `CronExpression = "* *"` | Validation fails |
| `ValidateFailsWhenCronExpressionHasInvalidChars` | Invalid characters | `CronExpression = "* * * * abc"` | Validation fails |
| `ValidateSucceedsWithStandard5PartCron` | 5-part cron | `CronExpression = "0 0 * * *"` | Passes |
| `ValidateSucceedsWithQuartz6PartCron` | 6-part cron | `CronExpression = "0 0 12 * * *"` | Passes |
| `ValidateSucceedsWithCronSpecialChars` | Special chars | `CronExpression = "0/15 * 1-5 * L"` | Passes |

#### Conditional Interval Rules

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `ValidateFailsWhenIntervalTypeHasNoIntervalSeconds` | Interval without seconds | `ServiceOptionType = "Interval"`, `IntervalSeconds = null` | Validation fails |
| `ValidateFailsWhenIntervalSecondsIsZero` | Zero interval | `IntervalSeconds = 0` | Validation fails |
| `ValidateFailsWhenIntervalSecondsIsNegative` | Negative interval | `IntervalSeconds = -1` | Validation fails |

#### TimeZoneId Validation

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `ValidateFailsWhenTimeZoneIdIsEmpty` | Required field | `TimeZoneId = ""` | Validation fails |

#### IValidateOptions Integration

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `ValidateOptionsReturnsSuccessForValidConfiguration` | IValidateOptions happy path | Valid config | `ValidateOptionsResult.Success` |
| `ValidateOptionsReturnsFailWithErrorMessages` | IValidateOptions failure | Invalid config | `ValidateOptionsResult.Fail` with error messages |

---

## 10. Endpoint Tests (Lightweight)

Endpoint tests verify request delegation and response mapping. They do NOT test
the full FastEndpoints pipeline -- they test the `HandleAsync` method directly by
mocking `ISchedulerService` and verifying the service is called with correct parameters.

**NOTE:** FastEndpoints endpoints are difficult to unit test directly because `Send.*`
methods depend on internal HttpContext state. The recommended approach is:
1. Mock `ISchedulerService`
2. Use FastEndpoints `Factory` testing utilities OR integration test with `WebApplicationFactory`
3. For this manifest, we document the behavior contracts for integration-style tests

### 10a. ListSchedulesEndpoint

**File:** `/home/mike/projects/FractalDataWorks/public/ReferenceSolutions/SchedulerServer/src/Reference.Scheduler.Server/Endpoints/ListSchedulesEndpoint.cs`

#### Dependencies to Mock

- `ISchedulerService` (Mock)

#### Test Cases

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `ListSchedulesReturns200WithScheduleList` | Happy path | Service returns success with list | 200 OK with schedule list |
| `ListSchedulesReturns400WhenServiceFails` | Service failure | Service returns failure | 400 with error |

### 10b. GetScheduleEndpoint

**File:** `/home/mike/projects/FractalDataWorks/public/ReferenceSolutions/SchedulerServer/src/Reference.Scheduler.Server/Endpoints/GetScheduleEndpoint.cs`

#### Dependencies to Mock

- `ISchedulerService` (Mock)

#### Test Cases

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `GetScheduleReturns200WhenFound` | Happy path | Service returns success | 200 OK with schedule |
| `GetScheduleReturns404WhenNotFound` | Not found | Service returns failure | 404 Not Found |

### 10c. CreateScheduleEndpoint

**File:** `/home/mike/projects/FractalDataWorks/public/ReferenceSolutions/SchedulerServer/src/Reference.Scheduler.Server/Endpoints/CreateScheduleEndpoint.cs`

#### Dependencies to Mock

- `ISchedulerService` (Mock)

#### Test Cases

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `CreateScheduleReturns201WithScheduleId` | Happy path | Service returns success | 201 with ScheduleId and Name |
| `CreateScheduleReturns400WhenServiceFails` | Service failure | Service returns failure | 400 with error |
| `CreateScheduleDelegatesAllFieldsToService` | Parameter passing | Request with Name, PipelineName, CronExpression | Service called with matching args |

### 10d. UpdateScheduleEndpoint

**File:** `/home/mike/projects/FractalDataWorks/public/ReferenceSolutions/SchedulerServer/src/Reference.Scheduler.Server/Endpoints/UpdateScheduleEndpoint.cs`

#### Dependencies to Mock

- `ISchedulerService` (Mock)

#### Test Cases

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `UpdateScheduleReturns200WhenSuccessful` | Happy path | Service returns success | 200 with Success=true |
| `UpdateScheduleReturns400WhenServiceFails` | Service failure | Service returns failure | 400 with error |
| `UpdateScheduleDefaultsSchedulerTypeToCron` | Default type | `SchedulerType = null` | Service called with `ServiceOptionType = "Cron"` |
| `UpdateScheduleDefaultsTimeZoneToUtc` | Default timezone | `TimeZoneId = null` | Service called with `TimeZoneId = "UTC"` |
| `UpdateScheduleDefaultsIsEnabledToTrue` | Default enabled | `IsEnabled = null` | Service called with `IsEnabled = true` |

### 10e. DeleteScheduleEndpoint

**File:** `/home/mike/projects/FractalDataWorks/public/ReferenceSolutions/SchedulerServer/src/Reference.Scheduler.Server/Endpoints/DeleteScheduleEndpoint.cs`

#### Dependencies to Mock

- `ISchedulerService` (Mock)

#### Test Cases

| Test Name | Branch/Scenario | Setup | Expected |
|-----------|----------------|-------|----------|
| `DeleteScheduleReturns204WhenSuccessful` | Happy path | Service returns success | 204 No Content |
| `DeleteScheduleReturns400WhenServiceFails` | Service failure | Service returns failure | 400 with error |
| `DeleteScheduleDelegatesNameToService` | Parameter passing | Request with Name | Service called with matching name |

---

## 11. ServiceCollectionExtensions

**File:** `/home/mike/projects/FractalDataWorks/public/ReferenceSolutions/SchedulerServer/src/Reference.Scheduler.Server/Extensions/ServiceCollectionExtensions.cs`

### Complexity Metrics

| Method | Complexity | Status |
|--------|------------|--------|
| `AddSchedulerServer` | 1 | Trivial (sequential registration) |

### Exclude from Coverage

This is orchestration code (complexity 1). All registrations are tested indirectly through
the service tests. Apply `[ExcludeFromCodeCoverage]` with justification
"Orchestration - tested via integration and service tests".

---

## 12. Logging Classes

### SchedulerServerLog, StartupLog, PreComputeLog

These are source-generated MessageLogging classes. All methods are `static partial` and
generated by the `MessageLogging.SourceGenerators`. They have complexity 1 (generated code).

**Exclude from coverage:** "Generated code - tested via generator tests"

---

## 13. Configuration Classes

### SchedulerConfiguration, EtlDispatchConfiguration, PreComputeOptions, ScheduleConfiguration

All are POCO classes with `{ get; set; }` properties. `ScheduleConfiguration` and
`SchedulerConfiguration` already have `[ExcludeFromCodeCoverage]`.

`PreComputeOptions` should also be excluded (complexity 1, no logic).
`EtlDispatchConfiguration` already has `[ExcludeFromCodeCoverage]`.

---

## Summary

### Test Count by Class

| Class | Tests | Priority |
|-------|-------|----------|
| CronJobScheduler | 12 | HIGH - core scheduling logic |
| IntervalJobScheduler | 13 | HIGH - core scheduling logic |
| ManualJobScheduler | 2 | LOW - trivial |
| OneTimeJobScheduler | 8 | HIGH - core scheduling logic |
| DefaultSchedulerService | 22 | HIGH - main business logic |
| EtlDispatchService (refactored) | 11 | HIGH - retry/backoff logic |
| SchedulerBackgroundService | 18 | MEDIUM - orchestration with complex branches |
| PreComputeCalculationsJob | 8 | MEDIUM - semaphore/timer logic |
| ScheduleConfigurationValidator | 24 | HIGH - validation rules |
| Endpoints (5 total) | 14 | MEDIUM - integration-style |
| **TOTAL** | **132** | |

### Excluded from Coverage

| Class/Method | Reason |
|--------------|--------|
| ServiceCollectionExtensions.AddSchedulerServer | Orchestration (complexity 1) |
| SchedulerServerLog (all methods) | Generated code |
| StartupLog (all methods) | Generated code |
| PreComputeLog (all methods) | Generated code |
| ScheduleConfiguration | Already `[ExcludeFromCodeCoverage]` |
| SchedulerConfiguration | Already `[ExcludeFromCodeCoverage]` |
| EtlDispatchConfiguration | Already `[ExcludeFromCodeCoverage]` |
| PreComputeOptions | POCO (complexity 1) - add `[ExcludeFromCodeCoverage]` |
| Program.Main | Startup orchestration - tested via integration |

### Testing Strategy Notes

1. **JobScheduler tests** are pure unit tests with no mocking needed. Instantiate directly
   since TypeCollection uses `RestrictToCurrentCompilation = false` here.

2. **DefaultSchedulerService tests** require careful mock setup for IDataGateway.
   The gateway is called with command objects -- verify command structure via `It.Is<>()`.

3. **EtlDispatchService tests** (post-refactoring) mock `IPipelineJobClient`.
   Use `Stopwatch` or task-based verification for backoff timing.
   For cancellation tests, use `CancellationTokenSource` with timed cancellation.

4. **SchedulerBackgroundService** is the most complex to test due to the infinite loop.
   Use `CancellationTokenSource` with short timeouts.
   Mock `IServiceScopeFactory` to return a scope that resolves mock services.

5. **Endpoint tests** should use FastEndpoints `Factory.Create<TEndpoint>()` or
   integration tests with `WebApplicationFactory<Program>`. Direct `HandleAsync` calls
   require HttpContext setup which is non-trivial.

6. **ScheduleConfigurationValidator** tests are straightforward FluentValidation tests.
   Create a `ScheduleConfiguration` with specific field values and assert on
   `validator.Validate(config)` results.
