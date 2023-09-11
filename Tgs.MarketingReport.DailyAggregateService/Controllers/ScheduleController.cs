using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Tgs.WebApi.Toolkits;

namespace Tgs.MarketingReport.DailyAggregateService.Controllers;

[Route("[controller]")]
[ApiController]
public class ScheduleController : ControllerBase
{
    [HttpPost("marketing/agent/{processDate}", Name = "QueueProcessAgentMarketingStatisticsWithProcessDate")]
    [SuccessStatusCode(202)]
    public void QueueProcessAgentMarketingStatisticsWithProcessDate(
        DateOnly processDate)
        => BackgroundJob.Enqueue<ScheduleBackgroundJob>(job => job.ExecuteAsync(
            processDate,
            "tgs.marketing.background.agent"));

    [HttpPost("marketing/brand/{processDate}", Name = "QueueProcessBrandMarketingStatisticsWithProcessDate")]
    [SuccessStatusCode(202)]
    public void QueueProcessBrandMarketingStatisticsWithProcessDate(
        DateOnly processDate)
        => BackgroundJob.Enqueue<ScheduleBackgroundJob>(job => job.ExecuteAsync(
            processDate,
            "tgs.marketing.background.brand"));

    [HttpPost("marketing", Name = "QueueProcessMarketingStatisticsJobs")]
    public void QueueProcessMarketingStatisticsJobs()
    {
        var currentDate = DateOnly.FromDateTime(DateTimeOffset.Now
            .ToOffset(TimeSpan.FromHours(8D))
            .DateTime);

        _ = BackgroundJob.Enqueue<ScheduleBackgroundJob>(job => job.ExecuteAsync(
            currentDate,
            "tgs.marketing.background.agent",
            "tgs.marketing.background.brand"));
    }

    [HttpPost("gaming/player/{processDate}", Name = "QueueProcessPlayerGamingStatisticsWithProcessDate")]
    [SuccessStatusCode(202)]
    public void QueueProcessPlayerGamingStatisticsWithProcessDate(
        DateOnly processDate)
        => BackgroundJob.Enqueue<ScheduleBackgroundJob>(job => job.ExecuteAsync(
            processDate,
            "tgs.gaming.background.player"));

    [HttpPost("register/player/{processDate}", Name = "QueueProcessPlayerRegisterStatisticsWithProcessDate")]
    [SuccessStatusCode(202)]
    public void QueueProcessPlayerRegisterStatisticsWithProcessDate(
        DateOnly processDate)
        => BackgroundJob.Enqueue<ScheduleBackgroundJob>(job => job.ExecuteAsync(
            processDate,
            "tgs.register.background.player"));
}
