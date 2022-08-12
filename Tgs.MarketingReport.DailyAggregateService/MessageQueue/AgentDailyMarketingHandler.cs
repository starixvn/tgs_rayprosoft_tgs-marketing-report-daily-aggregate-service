using Tgs.MarketingReport.DailyAggregate.Core;
using Tgs.MarketingReport.DailyAggregateService.Protos;
using Tgs.MessageQueue;

namespace Tgs.MarketingReport.DailyAggregateService.MessageQueue;

public class AgentDailyMarketingHandler : IMessageHandler
{
    private readonly IMessageSender _messageSender;
    private readonly ITgsRepository _tgsRepository;

    public AgentDailyMarketingHandler(ITgsRepository tgsRepository, IMessageSender messageSender)
    {
        _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
        _messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
    }

    public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var request = ProcessRequest.Parser.ParseFrom(data.Span);

        var requestDate = request.ProcessDate.ToDateOnly();

        if (requestDate is null)
            return;

        var playerRegisterProcessDate = await _tgsRepository
            .GetPlayerDailyRegisterStatisticsLastProcessDateAsync(cancellationToken)
            .ConfigureAwait(false);

        if (playerRegisterProcessDate is null || requestDate > playerRegisterProcessDate)
            await _messageSender
                .SendAsync("tgs.register.background.player", data, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

        var playerGamingProcessDate = await _tgsRepository
            .GetPlayerDailyGamingStatisticsLastProcessDateAsync(cancellationToken)
            .ConfigureAwait(false);

        if (playerGamingProcessDate is null || requestDate > playerGamingProcessDate)
            await _messageSender
                .SendAsync("tgs.gaming.background.player", data, cancellationToken)
                .ConfigureAwait(false);

        var lastProcessDate = await _tgsRepository
            .GetAgentDailyMarketingStatisticsLastProcessDateAsync(cancellationToken)
            .ConfigureAwait(false);

        if (lastProcessDate is null)
        {
            await _tgsRepository
                .ProcessAgentDailyMarketingStatisticsAsync(requestDate.Value, cancellationToken)
                .ConfigureAwait(false);
            await _tgsRepository
                .SetAgentDailyMarketingStatisticsLastProcessDateAsync(requestDate.Value, cancellationToken)
                .ConfigureAwait(false);
        }
        else if (requestDate > lastProcessDate)
        {
            var diffDays = (requestDate.Value.ToDateTime(TimeOnly.MinValue)
                - lastProcessDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays + 1;

            for (int i = 1; i < diffDays; i++)
                await _tgsRepository
                    .ProcessAgentDailyMarketingStatisticsAsync(
                        lastProcessDate.Value.AddDays(i),
                        cancellationToken)
                    .ConfigureAwait(false);

            await _tgsRepository
                .SetAgentDailyMarketingStatisticsLastProcessDateAsync(requestDate.Value, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            var diffDays = (lastProcessDate.Value.ToDateTime(TimeOnly.MinValue)
                - requestDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays + 1;

            for (int i = 0; i < diffDays; i++)
                await _tgsRepository
                    .ProcessAgentDailyMarketingStatisticsAsync(
                        requestDate.Value.AddDays(i),
                        cancellationToken)
                    .ConfigureAwait(false);
        }
    }
}
