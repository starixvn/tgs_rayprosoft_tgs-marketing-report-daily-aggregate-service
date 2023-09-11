namespace Tgs.MarketingReport.DailyAggregate.Core;

public interface ITgsRepository
{
    ValueTask GenerateDailyAggregateReportAsync(
        DateOnly processDate,
        CancellationToken cancellationToken = default);

    ValueTask<DateOnly?> GetAgentDailyMarketingStatisticsLastProcessDateAsync(CancellationToken cancellationToken = default);

    ValueTask<DateOnly?> GetBrandDailyMarketingStatisticsLastProcessDateAsync(CancellationToken cancellationToken = default);

    ValueTask<DateOnly?> GetPlayerDailyGamingStatisticsLastProcessDateAsync(CancellationToken cancellationToken = default);

    ValueTask<DateOnly?> GetPlayerDailyRegisterStatisticsLastProcessDateAsync(CancellationToken cancellationToken = default);

    ValueTask ProcessAgentDailyMarketingStatisticsAsync(DateOnly processDate, CancellationToken cancellationToken = default);

    ValueTask ProcessBrandDailyMarketingStatisticsAsync(DateOnly processDate, CancellationToken cancellationToken = default);

    ValueTask ProcessPlayerDailyGamingStatisticsAsync(DateOnly processDate, CancellationToken cancellationToken = default);

    ValueTask ProcessPlayerDailyRegisterStatisticsAsync(DateOnly processDate, CancellationToken cancellationToken = default);

    ValueTask SetAgentDailyMarketingStatisticsLastProcessDateAsync(DateOnly date, CancellationToken cancellationToken = default);

    ValueTask SetBrandDailyMarketingStatisticsLastProcessDateAsync(DateOnly date, CancellationToken cancellationToken = default);

    ValueTask SetPlayerDailyGamingStatisticsLastProcessDateAsync(DateOnly date, CancellationToken cancellationToken = default);

    ValueTask SetPlayerDailyRegisterStatisticsLastProcessDateAsync(DateOnly date, CancellationToken cancellationToken = default);
}
