namespace Tgs.Marketing.ReportDailyAggregateService.ViewModels;

public record GenerateDailyAggregateReportViewModel(
    DateOnly StartDate,
    DateOnly EndDate);
