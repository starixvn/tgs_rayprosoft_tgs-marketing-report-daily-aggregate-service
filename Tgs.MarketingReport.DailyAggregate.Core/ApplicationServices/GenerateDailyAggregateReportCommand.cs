namespace Tgs.MarketingReport.DailyAggregate.Core.ApplicationServices;

public record GenerateDailyAggregateReportCommand(DateOnly StartDate, DateOnly EndDate);
