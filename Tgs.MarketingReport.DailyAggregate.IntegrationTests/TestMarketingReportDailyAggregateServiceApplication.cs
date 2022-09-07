using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using Tgs.MarketingReport.DailyAggregate.Repositories.Entities;

namespace Tgs.MarketingReport.DailyAggregate.IntegrationTests;

extern alias MarketingReportDailAggregateSvc;

internal class TestMarketingReportDailyAggregateServiceApplication : WebApplicationFactory<MarketingReportDailAggregateSvc.Program>
{
    private readonly Action<IWebHostBuilder>? _webHostConfigure;

    public TestMarketingReportDailyAggregateServiceApplication(Action<IWebHostBuilder>? webHostConfigure = null)
    {
        _webHostConfigure = webHostConfigure;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _ = builder
            .ConfigureAppConfiguration(c => c.Sources.Clear())
            .ConfigureServices(services => services
                .AddSingleton<IConfigureOptions<OtlpExporterOptions>>(
                    sp => new ConfigureNamedOptions<OtlpExporterOptions>(
                        Options.DefaultName,
                        exporterOptions =>
                        {
                            exporterOptions.TimeoutMilliseconds = 0;
                        }))
                .Replace(ServiceDescriptor.Singleton(sp =>
                {
                    var dbContextOptionsBuilder = new DbContextOptionsBuilder<TgsContext>();
                    _ = dbContextOptionsBuilder
                        .UseInMemoryDatabase(Guid.NewGuid().ToString())
                        .ConfigureWarnings(x => x.Ignore(
                            InMemoryEventId.TransactionIgnoredWarning));

                    return new TgsContext(dbContextOptionsBuilder.Options);
                }))
                .AddFakeNatsMessageQueue());

        _webHostConfigure?.Invoke(builder);
    }
}
