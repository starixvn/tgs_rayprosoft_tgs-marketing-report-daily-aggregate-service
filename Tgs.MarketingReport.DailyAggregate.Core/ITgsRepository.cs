namespace Tgs.MarketingReport.DailyAggregate.Core;

public interface ITgsRepository
{
    ValueTask GenerateDailyAggregateReportAsync(
        DateOnly processDate,
        CancellationToken cancellationToken = default);

    ValueTask<DateOnly?> GetPlayerDailyGamingStatisticsLastProcessDateAsync(CancellationToken cancellationToken = default);

    ValueTask ProcessPlayerDailyGamingStatisticsAsync(DateOnly processDate, CancellationToken cancellationToken = default);

    ValueTask SetPlayerDailyGamingStatisticsLastProcessDateAsync(DateOnly date, CancellationToken cancellationToken = default);
}
