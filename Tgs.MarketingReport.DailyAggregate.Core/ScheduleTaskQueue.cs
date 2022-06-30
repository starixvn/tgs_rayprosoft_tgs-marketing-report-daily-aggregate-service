using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tgs.MarketingReport.DailyAggregate.Core;

internal class ScheduleTaskQueue
{
    private readonly BlockingCollection<ScheduleTask> _tasks;

    public void Enqueue(ScheduleTask task)
            => _tasks.Add(task);
}
