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
    public async ValueTask GenerateDailyAggregateReportAsync(
        [FromServices] ICommandService<AddScheduleTaskCommand> commandService,
        GenerateDailyAggregateReportViewModel viewModel)
    {
        var start = viewModel.StartDate;
        var end = viewModel.EndDate == viewModel.StartDate ? viewModel.EndDate.AddDays(1) : viewModel.EndDate;

        var diffDays = end.DayNumber - start.DayNumber + 1;

        for (var i = 1; i < diffDays; i++)
        {
            await commandService.ExecuteAsync(
                    new AddScheduleTaskCommand(start.AddDays(i)),
                    HttpContext.RequestAborted)
                .ConfigureAwait(false);
        }
    }

    [HttpPut("generate/daily", Name = "GenerateAggregateReportByDaily")]
    public ValueTask GenerateDailyAggregateReportAsync(
        [FromServices] ICommandService<AddScheduleTaskCommand> commandService)
    {
        var now = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8D));

        var processDate = DateOnly.FromDateTime(now.DateTime);

        return commandService.ExecuteAsync(
            new AddScheduleTaskCommand(processDate),
            HttpContext.RequestAborted);
    }
}
