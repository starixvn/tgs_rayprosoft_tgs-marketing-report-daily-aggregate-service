using Tgs.ServiceUtility;

namespace Tgs.MarketingReport.DailyAggregate.Core.ApplicationServices;

internal class AddScheduleTaskCommandService : ICommandService<AddScheduleTaskCommand>
{
    private readonly ScheduleTaskQueue _scheduleTaskQueue;

    public AddScheduleTaskCommandService(ScheduleTaskQueue scheduleTaskQueue)
    {
        _scheduleTaskQueue = scheduleTaskQueue ?? throw new ArgumentNullException(nameof(scheduleTaskQueue));
    }

    public ValueTask ExecuteAsync(AddScheduleTaskCommand command, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
