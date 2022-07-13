using Tgs.ServiceUtility;

namespace Tgs.MarketingReport.DailyAggregate.Core.ApplicationServices;

internal class GenerateDailyAggregateReportCommandService : ICommandService<GenerateDailyAggregateReportCommand>
{
    private static readonly TimeSpan _EightHourTimeSpan = TimeSpan.FromHours(8);

    private readonly ITgsRepository _tgsRepository;

    public GenerateDailyAggregateReportCommandService(
        ITgsRepository tgsRepository)
    {
        _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
    }

    public ValueTask ExecuteAsync(
        GenerateDailyAggregateReportCommand command,
        CancellationToken cancellationToken = default)
    {
        var startTime = GetDateTimeOffsetOfEightHourTimezone(command.StartDate);
        var endTime = command.EndDate == command.StartDate
            ? GetDateTimeOffsetOfEightHourTimezone(command.EndDate.AddDays(1))
            : GetDateTimeOffsetOfEightHourTimezone(command.EndDate);

        return _tgsRepository.GenerateDailyAggregateReportAsync(
            startTime.UtcDateTime.ToUniversalTime(),
            endTime.UtcDateTime.ToUniversalTime(),
            cancellationToken);
    }

    private static DateTimeOffset GetDateTimeOffsetOfEightHourTimezone(DateOnly date)
        => new DateTimeOffset(
            date.Year,
            date.Month,
            date.Day,
            0,
            0,
            0,
            _EightHourTimeSpan);
}
