using Microsoft.AspNetCore.Mvc;
using Tgs.MarketingReportDailyAggregate.Core.ApplicationServices;
using Tgs.MarketingReportDailyAggregateService.ViewModels;
using Tgs.ServiceUtility;

namespace Tgs.MarketingReportDailyAggregateService.Controllers;

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
