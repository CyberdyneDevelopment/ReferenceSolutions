using System;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Scheduling.Abstractions;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services;
using FdwReferenceSchedulers.Hangfire.Configuration;
using FdwReferenceSchedulers.Hangfire.Logging;
using FdwReferenceSchedulers.Hangfire.Services;

namespace FdwReferenceSchedulers.Hangfire;

/// <summary>
/// Factory that produces <see cref="HangfireSchedulingService"/> instances from a
/// <see cref="HangfireSchedulerConfiguration"/>.
/// </summary>
public sealed class HangfireSchedulingFactory
    : ServiceFactoryBase<IFrameworkSchedulingService, HangfireSchedulerConfiguration>, IHangfireSchedulingFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<HangfireSchedulingFactory> _logger;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly IDataGateway _dataGateway;

    /// <summary>Initializes a new instance of the <see cref="HangfireSchedulingFactory"/> class.</summary>
    public HangfireSchedulingFactory(
        ILoggerFactory loggerFactory,
        IRecurringJobManager recurringJobManager,
        IDataGateway dataGateway,
        ILogger<HangfireSchedulingFactory>? logger = null)
    {
        _loggerFactory = loggerFactory;
        _logger = logger ?? NullLogger<HangfireSchedulingFactory>.Instance;
        _recurringJobManager = recurringJobManager;
        _dataGateway = dataGateway;
    }

    /// <inheritdoc />
    public override IGenericResult<IFrameworkSchedulingService> Create(HangfireSchedulerConfiguration configuration)
    {
        if (configuration is null)
            return GenericResult<IFrameworkSchedulingService>.Failure(
                HangfireSchedulingLog.NativeCallFailed(_logger, new ArgumentNullException(nameof(configuration)), "(unknown)"));

        var serviceLogger = _loggerFactory.CreateLogger<HangfireSchedulingService>();
        var service = new HangfireSchedulingService(serviceLogger, _recurringJobManager, _dataGateway, configuration);
        return GenericResult<IFrameworkSchedulingService>.Success(service);
    }
}
