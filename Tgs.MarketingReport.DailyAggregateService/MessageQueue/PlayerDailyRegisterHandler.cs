using Tgs.MarketingReport.DailyAggregate.Core;
using Tgs.MarketingReport.DailyAggregateService.Protos;
using Valhalla.MessageQueue;

namespace Tgs.MarketingReport.DailyAggregateService.MessageQueue;

public class PlayerDailyRegisterHandler : IMessageHandler
{
    private readonly ITgsRepository _tgsRepository;

    public PlayerDailyRegisterHandler(ITgsRepository tgsRepository)
    {
        _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
    }

    public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var request = ProcessRequest.Parser.ParseFrom(data.Span);

        var requestDate = request.ProcessDate.ToDateOnly();

        if (requestDate is null)
            return;

        var lastProcessDate = await _tgsRepository
            .GetPlayerDailyRegisterStatisticsLastProcessDateAsync(cancellationToken)
            .ConfigureAwait(false);

        if (lastProcessDate is null)
        {
            await _tgsRepository
                .ProcessPlayerDailyRegisterStatisticsAsync(requestDate.Value, cancellationToken)
                .ConfigureAwait(false);
            await _tgsRepository
                .SetPlayerDailyRegisterStatisticsLastProcessDateAsync(requestDate.Value, cancellationToken)
                .ConfigureAwait(false);
        }
        else if (requestDate > lastProcessDate)
        {
            var diffDays = (requestDate.Value.ToDateTime(TimeOnly.MinValue)
                - lastProcessDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays + 1;

            for (int i = 1; i < diffDays; i++)
                await _tgsRepository
                    .ProcessPlayerDailyRegisterStatisticsAsync(
                        lastProcessDate.Value.AddDays(i),
                        cancellationToken)
                    .ConfigureAwait(false);

            await _tgsRepository
                .SetPlayerDailyRegisterStatisticsLastProcessDateAsync(requestDate.Value, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            var diffDays = (lastProcessDate.Value.ToDateTime(TimeOnly.MinValue)
                - requestDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays + 1;

            for (int i = 0; i < diffDays; i++)
                await _tgsRepository
                    .ProcessPlayerDailyRegisterStatisticsAsync(
                        requestDate.Value.AddDays(i),
                        cancellationToken)
                    .ConfigureAwait(false);
        }
    }
}
