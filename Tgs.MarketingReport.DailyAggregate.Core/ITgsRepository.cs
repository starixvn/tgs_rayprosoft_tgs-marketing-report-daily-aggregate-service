namespace Tgs.MarketingReport.DailyAggregate.Core;

public interface ITgsRepository
{
    ValueTask GenerateDailyAggregateReportAsync(
        DateOnly processDate,
        CancellationToken cancellationToken = default);
}
