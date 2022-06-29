using Microsoft.AspNetCore.Mvc;
using Tgs.MarketingReport.DailyAggregate.Core.ApplicationServices;
using Tgs.MarketingReport.DailyAggregateService.ViewModels;
using Tgs.ServiceUtility;

namespace Tgs.MarketingReport.DailyAggregateService.Controllers;

[Route("[controller]")]
[ApiController]
public class MarketingReportController : ControllerBase
{
    [HttpPut("generate", Name = "GenerateAggregateReportByDateRange")]
    public ValueTask GenerateDailyAggregateReportAsync(
        [FromServices] ICommandService<GenerateDailyAggregateReportCommand> commandService,
        GenerateDailyAggregateReportViewModel viewModel)
        => commandService.ExecuteAsync(
            new GenerateDailyAggregateReportCommand(viewModel.StartDate, viewModel.EndDate),
            HttpContext.RequestAborted);

    [HttpPut("generate/daily", Name = "GenerateAggregateReportByDaily")]
    public ValueTask GenerateDailyAggregateReportAsync(
        [FromServices] ICommandService<GenerateDailyAggregateReportCommand> commandService)
    {
        var now = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8D));

        var start = new DateOnly(now.Year, now.Month, now.Day);
        var end = start.AddDays(1);

        return commandService.ExecuteAsync(
            new GenerateDailyAggregateReportCommand(start, end),
            HttpContext.RequestAborted);
    }
}
