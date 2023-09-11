using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.MarketingReport.DailyAggregateService;
using Tgs.MarketingReport.DailyAggregateService.MessageQueue;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("/config/appsettings.json", true);

builder.Logging
    .AddOpenTelemetry(options =>
    {
        options.IncludeFormattedMessage = true;
        options.ParseStateValues = true;
        _ = options.AddFluentdForwardExporter(fluentOptions =>
        {
            var nodeIp = builder.Configuration.GetValue<string>("NODE_IP");
            fluentOptions.Host = nodeIp;
            fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME");
            fluentOptions.UseMessagePack();
        });
    });

builder.Services
    .AddMarketingReportDailyAggregateCore()
    .AddMarketingReportDailyAggregateRepositories((sp, dbBuilder) => dbBuilder
        .UseNpgsql("name=Tgs"));

builder.Services
    .AddMessageQueue()
    .AddNatsMessageQueue(config => config
        .ConfigQueueOptions((options, sp) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();

            options.Url = config.GetConnectionString("Nats");
        })
        .AddReplyHandler<PlayerDailyGamingHandler>("tgs.gaming.background.player", "tgs.gaming.background.player")
        .AddReplyHandler<PlayerDailyRegisterHandler>("tgs.register.background.player", "tgs.register.background.player")
        .AddReplyHandler<AgentDailyMarketingHandler>("tgs.marketing.background.agent", "tgs.marketing.background.agent")
        .AddReplyHandler<BrandDailyMarketingHandler>("tgs.marketing.background.brand", "tgs.marketing.background.brand"))
    .AddNatsGlobPatternExchange("*");

builder.Services
    .AddTransient<ScheduleBackgroundJob>()
    .AddHangfire((_, configuration) => configuration
        .UseMemoryStorage())
    .AddHangfireServer();

builder.Services
    .AddRouting(options => options.LowercaseUrls = true)
    .AddControllers(options => options.UseDateOnlyTimeOnlyStringConverters())
    .AddJsonOptions(options => options.UseDateOnlyTimeOnlyStringConverters());

builder.Services
    .AddSingleton<IConfigureOptions<OtlpExporterOptions>>(
        sp => new ConfigureNamedOptions<OtlpExporterOptions>(
            Options.DefaultName,
            exporterOptions =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var nodeIp = config.GetValue<string>("NODE_IP");

                if (!string.IsNullOrWhiteSpace(nodeIp))
                    exporterOptions.Endpoint = new Uri($"http://{nodeIp}:4317");

                config.GetSection("Opentelemetry:Tracing").Bind(exporterOptions);
            }))
    .AddOpenTelemetryTracing(traceBuilder => traceBuilder
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddEnvironmentVariableDetector())
        .AddSource("Tgs.*")
        .AddSource("TGS.*")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddProfileViewExporter()
        .AddOtlpExporter())
    .AddProfileViewer()
    .Configure<AspNetCoreInstrumentationOptions>(options =>
    {
        options.Filter = httpContext =>
        {
            return !httpContext.Request.Path.HasValue
                   || !httpContext.Request.Path.StartsWithSegments("/swagger")
                   && !httpContext.Request.Path.StartsWithSegments("/healthz")
                   && !httpContext.Request.Path.StartsWithSegments("/profiler/")
                   && !new[] { "html", "ico", "png", "jpg" }.Any(
                       ext => httpContext.Request.Path.Value.EndsWith($".{ext}"));
        };
    });

builder.Services.AddSwaggerGen(c => c.UseDateOnlyTimeOnlyStringConverters());

builder.Services
    .AddHealthChecks();

var app = builder.Build();

app.MapControllers();
app.MapProfileViewer();
app.MapSwagger();
app.UseSwaggerUI();

app.MapHealthChecks("/healthz");

app.Run();
