using Microsoft.Extensions.DependencyInjection;
using Tgs.MarketingReport.DailyAggregate.Client;
using Tgs.MarketingReport.DailyAggregate.Core;

namespace Tgs.MarketingReport.DailyAggregate.IntegrationTests;

public class MarketingReportTests
{
    [Test]
    public async Task Generate_Daily_Aggregate_Report()
    {
        await using var web = new TestMarketingReportDailyAggregateServiceApplication(builder =>
        {
            _ = builder.ConfigureServices(services =>
                services.AddTransient<ITgsRepository, StubTgsRepository>());
        });

        var httpClient = web.CreateClient();

        var sut = new MarketingReportDailyAggregateClient(httpClient);

        var startDate = DateOnly.Parse("2022-06-22");
        var endDate = DateOnly.Parse("2022-06-23");

        await sut.GenerateMarkingDailyAggregateReportAsync(startDate, endDate);
    }
}
