using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace Tgs.MarketingReport.DailyAggregateService.Controllers;

[Route("[controller]")]
[ApiController]
public class ScheduleController : ControllerBase
{
    [HttpPost("marketing/agent/{processDate}", Name = "QueueProcessAgentMarketingStatisticsWithProcessDate")]
    public void QueueProcessAgentMarketingStatisticsWithProcessDate(
        DateOnly processDate)
        => BackgroundJob.Enqueue<ScheduleBackgroundJob>(job => job.ExecuteAsync(
            processDate,
            "tgs.marketing.background.agent"));

    [HttpPost("gaming/player/{processDate}", Name = "QueueProcessPlayerGamingStatisticsWithProcessDate")]
    public void QueueProcessPlayerGamingStatisticsWithProcessDate(
        DateOnly processDate)
        => BackgroundJob.Enqueue<ScheduleBackgroundJob>(job => job.ExecuteAsync(
            processDate,
            "tgs.gaming.background.player"));

    [HttpPost("register/player/{processDate}", Name = "QueueProcessPlayerRegisterStatisticsWithProcessDate")]
    public void QueueProcessPlayerRegisterStatisticsWithProcessDate(
        DateOnly processDate)
        => BackgroundJob.Enqueue<ScheduleBackgroundJob>(job => job.ExecuteAsync(
            processDate,
            "tgs.register.background.player"));
}
