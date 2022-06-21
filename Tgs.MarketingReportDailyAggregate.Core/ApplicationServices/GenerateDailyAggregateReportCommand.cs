namespace Tgs.MarketingReportDailyAggregate.Core.ApplicationServices;

public record GenerateDailyAggregateReportCommand(DateOnly StartDate, DateOnly EndDate);
