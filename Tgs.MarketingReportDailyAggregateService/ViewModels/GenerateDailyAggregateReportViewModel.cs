namespace Tgs.MarketingReportDailyAggregateService.ViewModels;

public record GenerateDailyAggregateReportViewModel(
    DateOnly StartDate,
    DateOnly EndDate);
