extern alias MarketingReportDailAggregateSvc;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using MarketingReportDailAggregateSvc::Tgs.MarketingReport.DailyAggregateService.MessageQueue;
using MarketingReportDailAggregateSvc::Tgs.MarketingReport.DailyAggregateService.Protos;
using NSubstitute;
using Tgs.MarketingReport.DailyAggregate.Core;

namespace Tgs.MarketingReport.DailyAggregate.IntegrationTests;
public class PlayerDailyRegisterHandlerTests
{
    [Test]
    public async Task Process_Request_By_Daily()
    {
        var fakeTgsRepository = Substitute.For<ITgsRepository>();

        var sut = new PlayerDailyRegisterHandler(fakeTgsRepository);

        var request = new ProcessRequest
        {
            ProcessDate = DateOnly.Parse("2022-07-01")
        };

        await sut.HandleAsync(request.ToByteArray(), CancellationToken.None);

        _ = fakeTgsRepository.Received(1)
            .ProcessPlayerDailyRegisterStatisticsAsync(Arg.Is((DateOnly)request.ProcessDate));
        _ = fakeTgsRepository.Received(1)
            .SetPlayerDailyRegisterStatisticsLastProcessDateAsync(Arg.Is((DateOnly)request.ProcessDate));
    }

    [Test]
    public async Task Process_Request_By_Daily_Already_Has_ProcessDate()
    {
        var fakeTgsRepository = Substitute.For<ITgsRepository>();

        var sut = new PlayerDailyRegisterHandler(fakeTgsRepository);

        var request = new ProcessRequest
        {
            ProcessDate = DateOnly.Parse("2022-07-01")
        };

        _ = fakeTgsRepository.GetPlayerDailyGamingStatisticsLastProcessDateAsync()
            .Returns(DateOnly.Parse("2022-06-30"));

        await sut.HandleAsync(request.ToByteArray(), CancellationToken.None);

        _ = fakeTgsRepository.Received(1)
            .ProcessPlayerDailyRegisterStatisticsAsync(Arg.Is((DateOnly)request.ProcessDate));
        _ = fakeTgsRepository.Received(1)
            .SetPlayerDailyRegisterStatisticsLastProcessDateAsync(Arg.Is((DateOnly)request.ProcessDate));
    }

    [Test]
    public async Task Process_Request_RequestDate_Grant_Then_ProcessedDate_More()
    {
        var fakeTgsRepository = Substitute.For<ITgsRepository>();

        var sut = new PlayerDailyRegisterHandler(fakeTgsRepository);

        var request = new ProcessRequest
        {
            ProcessDate = DateOnly.Parse("2022-07-01")
        };

        _ = fakeTgsRepository.GetPlayerDailyRegisterStatisticsLastProcessDateAsync()
            .Returns(DateOnly.Parse("2022-06-28"));

        await sut.HandleAsync(request.ToByteArray(), CancellationToken.None);

        _ = fakeTgsRepository.Received(0)
            .ProcessPlayerDailyRegisterStatisticsAsync(Arg.Is(DateOnly.Parse("2022-6-28")));
        _ = fakeTgsRepository.Received(1)
            .ProcessPlayerDailyRegisterStatisticsAsync(Arg.Is(DateOnly.Parse("2022-6-29")));
        _ = fakeTgsRepository.Received(1)
            .ProcessPlayerDailyRegisterStatisticsAsync(Arg.Is(DateOnly.Parse("2022-6-30")));
        _ = fakeTgsRepository.Received(1)
            .ProcessPlayerDailyRegisterStatisticsAsync(Arg.Is(DateOnly.Parse("2022-7-1")));
        _ = fakeTgsRepository.Received(1)
            .SetPlayerDailyRegisterStatisticsLastProcessDateAsync(Arg.Is((DateOnly)request.ProcessDate));
    }

    [Test]
    public async Task Reprocess_PlayerDailyPaymentStatisics_Data()
    {
        var fakeTgsRepository = Substitute.For<ITgsRepository>();

        var sut = new PlayerDailyRegisterHandler(fakeTgsRepository);

        var request = new ProcessRequest
        {
            ProcessDate = DateOnly.Parse("2022-06-27")
        };

        _ = fakeTgsRepository.GetPlayerDailyRegisterStatisticsLastProcessDateAsync()
            .Returns(DateOnly.Parse("2022-06-30"));

        await sut.HandleAsync(request.ToByteArray(), CancellationToken.None);

        _ = fakeTgsRepository.Received(0)
            .SetPlayerDailyRegisterStatisticsLastProcessDateAsync(Arg.Is((DateOnly)request.ProcessDate));

        _ = fakeTgsRepository.Received(1)
            .ProcessPlayerDailyRegisterStatisticsAsync(Arg.Is(DateOnly.Parse("2022-06-27")));
        _ = fakeTgsRepository.Received(1)
            .ProcessPlayerDailyRegisterStatisticsAsync(Arg.Is(DateOnly.Parse("2022-06-28")));
        _ = fakeTgsRepository.Received(1)
            .ProcessPlayerDailyRegisterStatisticsAsync(Arg.Is(DateOnly.Parse("2022-06-29")));
        _ = fakeTgsRepository.Received(1)
            .ProcessPlayerDailyRegisterStatisticsAsync(Arg.Is(DateOnly.Parse("2022-06-30")));
    }
}
