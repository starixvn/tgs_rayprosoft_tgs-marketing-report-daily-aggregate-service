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
        [FromServices] ICommandService<AddScheduleTaskCommand> commandService,
        GenerateDailyAggregateReportViewModel viewModel)
        => commandService.ExecuteAsync(
            new AddScheduleTaskCommand(viewModel.StartDate, viewModel.EndDate),
            HttpContext.RequestAborted);

    [HttpPut("generate/daily", Name = "GenerateAggregateReportByDaily")]
    public ValueTask GenerateDailyAggregateReportAsync(
        [FromServices] ICommandService<AddScheduleTaskCommand> commandService)
    {
        var now = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8D));

        var end = DateOnly.FromDateTime(now.DateTime);
        var start = end.AddDays(-1);

        return commandService.ExecuteAsync(
            new AddScheduleTaskCommand(start, end),
            HttpContext.RequestAborted);
    }
}
