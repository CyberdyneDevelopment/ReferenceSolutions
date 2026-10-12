using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Scheduling.Abstractions;
using Quartz;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceSchedulers.Quartz.Configuration;
using FdwReferenceSchedulers.Quartz.Logging;
using FdwReferenceSchedulers.Quartz.Services;
using Fdw.Services;
using System;

namespace FdwReferenceSchedulers.Quartz;

/// <summary>
/// Factory that produces <see cref="QuartzSchedulingService"/> instances from a
/// <see cref="QuartzSchedulerConfiguration"/>.
/// </summary>
public sealed class QuartzSchedulingFactory
    : ServiceFactoryBase<IFrameworkSchedulingService, QuartzSchedulerConfiguration>, IQuartzSchedulingFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<QuartzSchedulingFactory> _logger;
    private readonly ISchedulerFactory _quartzSchedulerFactory;
    private readonly IDataGateway _dataGateway;

    /// <summary>Initializes a new instance of the <see cref="QuartzSchedulingFactory"/> class.</summary>
    public QuartzSchedulingFactory(
        ILoggerFactory loggerFactory,
        ISchedulerFactory quartzSchedulerFactory,
        IDataGateway dataGateway,
        ILogger<QuartzSchedulingFactory>? logger = null)
    {
        _loggerFactory = loggerFactory;
        _logger = logger ?? NullLogger<QuartzSchedulingFactory>.Instance;
        _quartzSchedulerFactory = quartzSchedulerFactory;
        _dataGateway = dataGateway;
    }

    /// <inheritdoc />
    public override IGenericResult<IFrameworkSchedulingService> Create(QuartzSchedulerConfiguration configuration)
    {
        if (configuration is null)
            return GenericResult<IFrameworkSchedulingService>.Failure(
                QuartzSchedulingLog.NativeCallFailed(_logger, new ArgumentNullException(nameof(configuration)), "(unknown)"));

        // Why GetAwaiter().GetResult(): IServiceFactory.Create is a synchronous contract shared by
        // every domain, but Quartz's own ISchedulerFactory.GetScheduler() is async. The scheduler
        // itself is a singleton Quartz already keeps warm, so this resolves immediately in practice.
        var scheduler = _quartzSchedulerFactory.GetScheduler().GetAwaiter().GetResult();

        var serviceLogger = _loggerFactory.CreateLogger<QuartzSchedulingService>();
        var service = new QuartzSchedulingService(serviceLogger, scheduler, _dataGateway, configuration);
        return GenericResult<IFrameworkSchedulingService>.Success(service);
    }
}
