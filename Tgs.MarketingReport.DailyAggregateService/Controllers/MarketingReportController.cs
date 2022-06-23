using Microsoft.AspNetCore.Mvc;
using Tgs.MarketingReport.DailyAggregate.Core.ApplicationServices;
using Tgs.MarketingReport.DailyAggregateService.ViewModels;
using Tgs.ServiceUtility;

namespace Tgs.MarketingReport.DailyAggregateService.Controllers;

[Route("[controller]")]
[ApiController]
public class MarketingReportController : ControllerBase
{
    [HttpPut("generate", Name = "GenerateDailyAggregateReport")]
    public ValueTask GenerateDailyAggregateReportAsync(
        [FromServices] ICommandService<GenerateDailyAggregateReportCommand> commandService,
        GenerateDailyAggregateReportViewModel viewModel)
        => commandService.ExecuteAsync(
            new GenerateDailyAggregateReportCommand(viewModel.StartDate, viewModel.EndDate),
            HttpContext.RequestAborted);
}
