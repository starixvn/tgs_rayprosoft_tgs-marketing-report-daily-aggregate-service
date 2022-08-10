using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace Tgs.MarketingReport.DailyAggregateService.Controllers;

[Route("[controller]")]
[ApiController]
public class ScheduleController : ControllerBase
{
    [HttpPost("gaming/player/{processDate}", Name = "QueueProcessPlayerGamingStatisticsWithProcessDate")]
    public void QueueProcessPlayerGamingStatisticsWithProcessDate(
        DateOnly processDate)
    {
        _ = BackgroundJob.Enqueue<ScheduleBackgroundJob>(job => job.ExecuteAsync(
            processDate,
            "tgs.gaming.background.player"));
    }
}
