using NSubstitute;
using Tgs.MarketingReportDailyAggregate.Core.ApplicationServices;

namespace Tgs.MarketingReportDailyAggregate.Core.UnitTests;

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
