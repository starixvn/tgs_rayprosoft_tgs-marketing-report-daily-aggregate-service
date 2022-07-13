using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tgs.MarketingReport.DailyAggregate.Core.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.MarketingReport.DailyAggregate.Core;

internal class ScheduleTaskQueue
{
    private readonly ILogger<ScheduleTaskQueue> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly BlockingCollection<ScheduleTask> _tasks = new();
    private Task? _backgroundTask;
    private CancellationTokenSource? _cancellationSource;

    public ScheduleTaskQueue(
        IServiceProvider serviceProvider,
        ILogger<ScheduleTaskQueue> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Enqueue(ScheduleTask task)
        => _tasks.Add(task);

    public void StartProcess(CancellationToken cancellationToken)
    {
        if (_tasks.IsCompleted)
            return;

        _cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _ = _cancellationSource.Token.Register(() => _tasks.CompleteAdding());

        _backgroundTask = Task.Run(async () =>
        {
            foreach (var scheduleTask in _tasks.GetConsumingEnumerable(_cancellationSource.Token))
            {
                await HandleScheduleTaskAsync(
                    scheduleTask,
                    _cancellationSource.Token).ConfigureAwait(false);
            }
        }, cancellationToken);
    }

    public void StopProcess()
    {
        if (_cancellationSource?.IsCancellationRequested == false)
            _cancellationSource.Cancel();

        try
        {
            if (_tasks.TryGetNonEnumeratedCount(out var count)
                && count > 0
                && _backgroundTask?.IsCompleted == false)
                _backgroundTask?.Wait();
        }
        catch (AggregateException ex) when (ex.InnerException is TaskCanceledException)
        {
        }
    }

    private async ValueTask HandleScheduleTaskAsync(ScheduleTask scheduleTask, CancellationToken cancellationToken)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            using var scope = _serviceProvider.CreateScope();

            var commandService =
                scope.ServiceProvider.GetRequiredService<ICommandService<GenerateDailyAggregateReportCommand>>();

            var command = new GenerateDailyAggregateReportCommand(scheduleTask.StartDate, scheduleTask.EndDate);

            await commandService.ExecuteAsync(command, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Log(
                LogLevel.Error,
                new EventId(10001),
                scheduleTask,
                ex,
                (state, _) => $"Process schedule task (start: {state.StartDate}, end: {state.EndDate}) occur error.");
        }
    }
}
