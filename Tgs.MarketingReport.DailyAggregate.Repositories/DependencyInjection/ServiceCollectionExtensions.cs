using Microsoft.EntityFrameworkCore;
using Tgs.MarketingReport.DailyAggregate.Core;
using Tgs.MarketingReport.DailyAggregate.Repositories;
using Tgs.MarketingReport.DailyAggregate.Repositories.Entities;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMarketingReportDailyAggregateRepositories(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> dbConfigure)
        => services
            .AddDbContext<TgsContext>(dbConfigure)
            .AddTransient<ITgsRepository, TgsRepository>();
}
