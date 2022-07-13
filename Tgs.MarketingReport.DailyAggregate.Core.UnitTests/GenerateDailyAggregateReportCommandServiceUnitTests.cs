using NSubstitute;
using Tgs.MarketingReport.DailyAggregate.Core.ApplicationServices;

namespace Tgs.MarketingReport.DailyAggregate.Core.UnitTests;

internal class GenerateDailyAggregateReportCommandServiceUnitTests
{
    [Test]
    public async Task Can_Generate_DailyAggregateReport()
    {
        var fakeRepository = Substitute.For<ITgsRepository>();

        var processDate = DateOnly.Parse("2022-06-20");

        var command = new GenerateDailyAggregateReportCommand(processDate);

        var sut = new GenerateDailyAggregateReportCommandService(fakeRepository);

        await sut.ExecuteAsync(command).ConfigureAwait(false);

        _ = fakeRepository
            .Received(1)
            .GenerateDailyAggregateReportAsync(Arg.Is(processDate));
    }
}
