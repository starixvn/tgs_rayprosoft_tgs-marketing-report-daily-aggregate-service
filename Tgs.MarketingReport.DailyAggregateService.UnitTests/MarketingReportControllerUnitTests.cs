using Microsoft.AspNetCore.Http;
using NSubstitute;
using Tgs.MarketingReport.DailyAggregate.Core.ApplicationServices;
using Tgs.MarketingReport.DailyAggregateService.Controllers;
using Tgs.MarketingReport.DailyAggregateService.ViewModels;
using Tgs.ServiceUtility;

namespace Tgs.MarketingReport.DailyAggregateService.UnitTests;

public class MarketingReportControllerUnitTests
{
    [Test]
    public async Task Generate_Daily_Aggregate_Report_By_Daily()
    {
        var fakeCommandService = Substitute.For<ICommandService<AddScheduleTaskCommand>>();

        var processDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8)).DateTime);

        var sut = new MarketingReportController
        {
            ControllerContext =
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestAborted = CancellationToken.None
                }
            }
        };

        await sut.GenerateDailyAggregateReportAsync(fakeCommandService);

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

        var sut = new MarketingReportController
        {
            ControllerContext =
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestAborted = CancellationToken.None
                }
            }
        };

        var startDate = DateOnly.Parse("2022-06-22");
        var endDate = DateOnly.Parse("2022-06-25");
        var viewModel = new GenerateDailyAggregateReportViewModel(startDate, endDate);

        await sut.GenerateDailyAggregateReportAsync(fakeCommandService, viewModel);

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
