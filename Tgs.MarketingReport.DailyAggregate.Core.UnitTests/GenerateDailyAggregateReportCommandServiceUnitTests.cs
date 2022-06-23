using NSubstitute;
using Tgs.MarketingReport.DailyAggregate.Core.ApplicationServices;
using Tgs.MarketingReport.DailyAggregate.Core;

namespace Tgs.MarketingReport.DailyAggregate.Core.UnitTests;

internal class GenerateDailyAggregateReportCommandServiceUnitTests
{
    [Test]
    public async Task Can_Generate_DailyAggregateReport()
    {
        var fakeRepository = Substitute.For<ITgsRepository>();

        var start = DateOnly.Parse("2022-06-20");
        var end = DateOnly.Parse("2022-06-21");

        var command = new GenerateDailyAggregateReportCommand(start, end);

        var sut = new GenerateDailyAggregateReportCommandService(fakeRepository);

        await sut.ExecuteAsync(command).ConfigureAwait(false);

        _ = fakeRepository
            .Received(1)
            .GenerateDailyAggregateReportAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>());
    }
}
