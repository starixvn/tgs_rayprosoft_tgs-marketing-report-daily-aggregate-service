using Google.Protobuf;
using Tgs.MarketingReport.DailyAggregateService.Protos;
using Tgs.MessageQueue;

namespace Tgs.MarketingReport.DailyAggregateService;

public class ScheduleBackgroundJob
{
    private readonly IMessageSender _messageSender;

    public ScheduleBackgroundJob(IMessageSender messageSender)
    {
        _messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
    }

    public async Task ExecuteAsync(DateOnly processDate, params string[] subjects)
    {
        var request = new ProcessRequest
        {
            ProcessDate = processDate
        };

        var sendData = request.ToByteArray();

        foreach (var subject in subjects)
            await _messageSender
                .SendAsync(subject, sendData)
                .ConfigureAwait(false);
    }
}
