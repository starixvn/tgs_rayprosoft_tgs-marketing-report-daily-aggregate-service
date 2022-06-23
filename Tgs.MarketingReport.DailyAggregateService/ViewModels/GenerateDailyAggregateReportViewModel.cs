namespace Tgs.MarketingReport.DailyAggregateService.ViewModels;

public record GenerateDailyAggregateReportViewModel(
    DateOnly StartDate,
    DateOnly EndDate);
