// ReSharper disable CheckNamespace

using System.Reflection;
using Tgs.MarketingReport.DailyAggregate.Core;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMarketingReportDailyAggregateCore(this IServiceCollection services)
        => services
            .RegisterAllServiceUtilityTypes(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient)
            .AddSingleton<ScheduleTaskQueue>()
            .AddHostedService<ScheduleTaskBackgroundService>();
}
