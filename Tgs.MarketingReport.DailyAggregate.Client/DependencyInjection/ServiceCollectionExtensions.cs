using Microsoft.Extensions.Options;
using Tgs.MarketingReport.DailyAggregate.Client;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMarketingReportDailyAggregateClient(
        this IServiceCollection services,
        Action<MarketingReportDailyAggregateClientOptions, IServiceProvider> configure)
        => services.AddOptions<MarketingReportDailyAggregateClientOptions>()
            .Configure(configure)
            .Services
            .AddHttpClient<IMarketingReportDailyAggregateClient, MarketingReportDailyAggregateClient>()
            .ConfigureHttpClient((sp, httpClient) =>
            {
                var options = sp.GetRequiredService<IOptions<MarketingReportDailyAggregateClientOptions>>().Value;
                httpClient.BaseAddress = options.BaseUrl;
            })
            .Services;
}
