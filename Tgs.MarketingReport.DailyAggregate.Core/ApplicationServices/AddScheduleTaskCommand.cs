namespace Tgs.MarketingReport.DailyAggregate.Core.ApplicationServices;

public record AddScheduleTaskCommand(
    DateOnly StartDate,
    DateOnly EndDate);
