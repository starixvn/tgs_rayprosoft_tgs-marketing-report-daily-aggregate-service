using Microsoft.EntityFrameworkCore;
using Tgs.MarketingReportDailyAggregate.Core;
using Tgs.MarketingReportDailyAggregate.Repositories;
using Tgs.MarketingReportDailyAggregate.Repositories.Entities;

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
