using Microsoft.Extensions.Hosting;

namespace Tgs.MarketingReport.DailyAggregate.Core;

internal class ScheduleTaskBackgroundService : BackgroundService
{
    private readonly ScheduleTaskQueue _queue;

    public ScheduleTaskBackgroundService(ScheduleTaskQueue queue)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = stoppingToken.Register(() => _queue.StopProcess());

        _queue.StartProcess(stoppingToken);

        return Task.CompletedTask;
    }
}
