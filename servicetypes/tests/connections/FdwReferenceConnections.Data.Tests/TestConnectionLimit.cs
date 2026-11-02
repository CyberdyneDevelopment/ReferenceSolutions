using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Data.Tests;

// ConnectionLimitConfiguration's constructor is protected and its Enforce* members are virtual, so a
// limit is expressed by deriving and overriding what it enforces.
internal sealed class TestConnectionLimit : ConnectionLimitConfiguration
{
    private readonly int? _perSecond;
    private readonly int? _burst;
    private readonly int? _maxRows;
    private readonly int? _timeoutSeconds;
    private readonly int? _perDay;
    private readonly int? _concurrent;

    public TestConnectionLimit(
        Guid connectionId,
        int? perSecond = null,
        int? burst = null,
        int? maxRows = null,
        int? timeoutSeconds = null,
        int? perDay = null,
        int? concurrent = null)
    {
        ConnectionConfigurationId = connectionId;
        _perSecond = perSecond;
        _burst = burst;
        _maxRows = maxRows;
        _timeoutSeconds = timeoutSeconds;
        _perDay = perDay;
        _concurrent = concurrent;
    }

    public override int? EnforceMaxPerSecond => _perSecond;

    public override int? EnforceBurstSize => _burst;

    public override int? EnforceMaxRows => _maxRows;

    public override int? EnforceTimeoutSeconds => _timeoutSeconds;

    public override int? EnforceMaxQueriesPerDay => _perDay;

    public override int? EnforceMaxConcurrent => _concurrent;
}
