namespace Tgs.MarketingReport.DailyAggregate.Core;

public interface ITgsRepository
{
    ValueTask GenerateDailyAggregateReportAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);
}
