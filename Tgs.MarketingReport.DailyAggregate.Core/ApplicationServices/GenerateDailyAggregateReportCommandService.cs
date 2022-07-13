using Tgs.ServiceUtility;

namespace Tgs.MarketingReport.DailyAggregate.Core.ApplicationServices;

internal class GenerateDailyAggregateReportCommandService : ICommandService<GenerateDailyAggregateReportCommand>
{
    private readonly ITgsRepository _tgsRepository;

    public GenerateDailyAggregateReportCommandService(
        ITgsRepository tgsRepository)
    {
        _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
    }

    public ValueTask ExecuteAsync(
        GenerateDailyAggregateReportCommand command,
        CancellationToken cancellationToken = default)
        => _tgsRepository
            .GenerateDailyAggregateReportAsync(
                command.ProcessDate,
                cancellationToken);
}
