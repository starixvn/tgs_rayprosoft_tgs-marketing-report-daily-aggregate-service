using Tgs.MarketingReport.DailyAggregate.Core;

namespace Tgs.MarketingReport.DailyAggregate.IntegrationTests;

internal class StubTgsRepository : ITgsRepository
{
    public ValueTask GenerateDailyAggregateReportAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;
}
