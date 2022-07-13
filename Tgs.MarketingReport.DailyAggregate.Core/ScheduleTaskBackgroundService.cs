using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        return Task.Run(async () =>
        {
            while (true)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                _queue.StartProcess(stoppingToken);

                await Task.Delay(5000, stoppingToken).ConfigureAwait(false);
            }
        }, stoppingToken);
    }
}
