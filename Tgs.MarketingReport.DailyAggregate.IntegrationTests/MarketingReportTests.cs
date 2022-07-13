using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tgs.MarketingReport.DailyAggregate.Client;
using Tgs.MarketingReport.DailyAggregate.Core;
using Tgs.MarketingReport.DailyAggregate.Core.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.MarketingReport.DailyAggregate.IntegrationTests;

public class MarketingReportTests
{
    [Test]
    public async Task Generate_Daily_Aggregate_Report_By_Daily()
    {
        var fakeCommandService = Substitute.For<ICommandService<AddScheduleTaskCommand>>();
        var fakeRepository = Substitute.For<ITgsRepository>();

        await using var web = new TestMarketingReportDailyAggregateServiceApplication(builder =>
        {
            _ = builder.ConfigureServices(services =>
                services.AddTransient(sp => fakeRepository)
                    .AddTransient(sp => fakeCommandService));
        });

        var httpClient = web.CreateClient();

        var processDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8)).DateTime);

        var sut = new MarketingReportDailyAggregateClient(httpClient);

        await sut.GenerateMarkingDailyAggregateReportByDailyAsync();

        _ = fakeCommandService
            .Received(1)
            .ExecuteAsync(
                Arg.Is<AddScheduleTaskCommand>(x => x.ProcessDate == processDate),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Generate_Daily_Aggregate_Report_By_Date_Range()
    {
        var fakeCommandService = Substitute.For<ICommandService<AddScheduleTaskCommand>>();
        var fakeRepository = Substitute.For<ITgsRepository>();

        await using var web = new TestMarketingReportDailyAggregateServiceApplication(builder =>
        {
            _ = builder.ConfigureServices(services =>
                services.AddTransient(sp => fakeRepository)
                    .AddTransient(sp => fakeCommandService));
        });

        var httpClient = web.CreateClient();

        var sut = new MarketingReportDailyAggregateClient(httpClient);

        var startDate = DateOnly.Parse("2022-06-22");
        var endDate = DateOnly.Parse("2022-06-25");

        await sut.GenerateMarkingDailyAggregateReportAsync(startDate, endDate);

        _ = fakeCommandService
            .Received(1)
            .ExecuteAsync(
                Arg.Is<AddScheduleTaskCommand>(x => x.ProcessDate == DateOnly.Parse("2022-06-23")),
                Arg.Any<CancellationToken>());

        _ = fakeCommandService
            .Received(1)
            .ExecuteAsync(
                Arg.Is<AddScheduleTaskCommand>(x => x.ProcessDate == DateOnly.Parse("2022-06-24")),
                Arg.Any<CancellationToken>());

        _ = fakeCommandService
            .Received(1)
            .ExecuteAsync(
                Arg.Is<AddScheduleTaskCommand>(x => x.ProcessDate == DateOnly.Parse("2022-06-25")),
                Arg.Any<CancellationToken>());
    }
}
