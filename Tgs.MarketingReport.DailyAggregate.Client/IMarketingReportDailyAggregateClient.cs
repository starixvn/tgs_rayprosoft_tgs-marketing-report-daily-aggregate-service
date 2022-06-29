namespace Tgs.MarketingReport.DailyAggregate.Client;

public interface IMarketingReportDailyAggregateClient
{
    ValueTask GenerateMarkingDailyAggregateReportAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    ValueTask GenerateMarkingDailyAggregateReportByDailyAsync(
        CancellationToken cancellationToken = default);
}
