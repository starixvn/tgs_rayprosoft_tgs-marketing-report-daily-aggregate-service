extern alias MarketingReportDailAggregateSvc;

using Google.Protobuf;
using MarketingReportDailAggregateSvc::Tgs.MarketingReport.DailyAggregateService.MessageQueue;
using MarketingReportDailAggregateSvc::Tgs.MarketingReport.DailyAggregateService.Protos;
using NSubstitute;
using Tgs.MarketingReport.DailyAggregate.Core;
using Tgs.MessageQueue;

namespace Tgs.MarketingReport.DailyAggregate.IntegrationTests;

public class AgentDailyMarketingHandlerTests
{
    [Test]
    public async Task Process_Request_By_Daily()
    {
        var fakeTgsRepository = Substitute.For<ITgsRepository>();
        var fakeMessageSender = Substitute.For<IMessageSender>();

        var sut = new AgentDailyMarketingHandler(fakeTgsRepository, fakeMessageSender);

        var request = new ProcessRequest
        {
            ProcessDate = DateOnly.Parse("2022-07-01")
        };

        await sut.HandleAsync(request.ToByteArray(), CancellationToken.None);

        _ = fakeTgsRepository.Received(1)
            .ProcessAgentDailyMarketingStatisticsAsync(Arg.Is((DateOnly)request.ProcessDate));
        _ = fakeTgsRepository.Received(1)
            .SetAgentDailyMarketingStatisticsLastProcessDateAsync(Arg.Is((DateOnly)request.ProcessDate));
        _ = fakeMessageSender.Received(1)
            .SendAsync(
                Arg.Is("tgs.register.background.player"),
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<IEnumerable<MessageHeaderValue>>());
        _ = fakeMessageSender.Received(1)
            .SendAsync(
                Arg.Is("tgs.gaming.background.player"),
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<IEnumerable<MessageHeaderValue>>());
    }

    [Test]
    public async Task Process_Request_By_Daily_Already_Has_ProcessDate()
    {
        var fakeTgsRepository = Substitute.For<ITgsRepository>();
        var fakeMessageSender = Substitute.For<IMessageSender>();

        var sut = new AgentDailyMarketingHandler(fakeTgsRepository, fakeMessageSender);

        var request = new ProcessRequest
        {
            ProcessDate = DateOnly.Parse("2022-07-01")
        };

        _ = fakeTgsRepository.GetAgentDailyMarketingStatisticsLastProcessDateAsync()
            .Returns(DateOnly.Parse("2022-06-30"));

        await sut.HandleAsync(request.ToByteArray(), CancellationToken.None);

        _ = fakeTgsRepository.Received(1)
            .ProcessAgentDailyMarketingStatisticsAsync(Arg.Is((DateOnly)request.ProcessDate));
        _ = fakeTgsRepository.Received(1)
            .SetAgentDailyMarketingStatisticsLastProcessDateAsync(Arg.Is((DateOnly)request.ProcessDate));
    }

    [Test]
    public async Task Process_Request_By_RequestDate_Grant_Then_ProcessedDate_More()
    {
        var fakeTgsRepository = Substitute.For<ITgsRepository>();
        var fakeMessageSender = Substitute.For<IMessageSender>();

        var sut = new AgentDailyMarketingHandler(fakeTgsRepository, fakeMessageSender);

        var request = new ProcessRequest
        {
            ProcessDate = DateOnly.Parse("2022-07-01")
        };

        _ = fakeTgsRepository.GetAgentDailyMarketingStatisticsLastProcessDateAsync()
            .Returns(DateOnly.Parse("2022-06-28"));

        await sut.HandleAsync(request.ToByteArray(), CancellationToken.None);

        _ = fakeTgsRepository.Received(0)
            .ProcessAgentDailyMarketingStatisticsAsync(Arg.Is(DateOnly.Parse("2022-6-28")));
        _ = fakeTgsRepository.Received(1)
            .ProcessAgentDailyMarketingStatisticsAsync(Arg.Is(DateOnly.Parse("2022-6-29")));
        _ = fakeTgsRepository.Received(1)
            .ProcessAgentDailyMarketingStatisticsAsync(Arg.Is(DateOnly.Parse("2022-6-30")));
        _ = fakeTgsRepository.Received(1)
            .ProcessAgentDailyMarketingStatisticsAsync(Arg.Is(DateOnly.Parse("2022-7-1")));
        _ = fakeTgsRepository.Received(1)
            .SetAgentDailyMarketingStatisticsLastProcessDateAsync(Arg.Is((DateOnly)request.ProcessDate));
    }

    [Test]
    public async Task Reprocess_PlayerDailyPaymentStatisics_Data()
    {
        var fakeTgsRepository = Substitute.For<ITgsRepository>();
        var fakeMessageSender = Substitute.For<IMessageSender>();

        var sut = new AgentDailyMarketingHandler(fakeTgsRepository, fakeMessageSender);

        var request = new ProcessRequest
        {
            ProcessDate = DateOnly.Parse("2022-06-27")
        };

        _ = fakeTgsRepository.GetAgentDailyMarketingStatisticsLastProcessDateAsync()
            .Returns(DateOnly.Parse("2022-06-30"));

        await sut.HandleAsync(request.ToByteArray(), CancellationToken.None);

        _ = fakeTgsRepository.Received(0)
            .SetAgentDailyMarketingStatisticsLastProcessDateAsync(Arg.Is((DateOnly)request.ProcessDate));

        _ = fakeTgsRepository.Received(1)
            .ProcessAgentDailyMarketingStatisticsAsync(Arg.Is(DateOnly.Parse("2022-06-27")));
        _ = fakeTgsRepository.Received(1)
            .ProcessAgentDailyMarketingStatisticsAsync(Arg.Is(DateOnly.Parse("2022-06-28")));
        _ = fakeTgsRepository.Received(1)
            .ProcessAgentDailyMarketingStatisticsAsync(Arg.Is(DateOnly.Parse("2022-06-29")));
        _ = fakeTgsRepository.Received(1)
            .ProcessAgentDailyMarketingStatisticsAsync(Arg.Is(DateOnly.Parse("2022-06-30")));
    }
}
