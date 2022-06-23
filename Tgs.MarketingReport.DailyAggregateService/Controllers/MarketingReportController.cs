using Microsoft.AspNetCore.Mvc;
using Tgs.ServiceUtility;
using Tgs.Marketing.ReportDailyAggregateService.ViewModels;
using Tgs.MarketingReport.DailyAggregate.Core.ApplicationServices;

namespace Tgs.Marketing.ReportDailyAggregateService.Controllers;

[Route("api/[controller]")]
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
