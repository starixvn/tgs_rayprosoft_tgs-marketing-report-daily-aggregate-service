using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("/config/appsettings.json", true);

builder.Services
    .AddMarketingReportDailyAggregateCore()
    .AddMarketingReportDailyAggregateRepositories((sp, dbBuilder) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();

        _ = dbBuilder.UseNpgsql(config.GetConnectionString("Tgs"));
    });

builder.Services
    .AddRouting(options => options.LowercaseUrls = true)
    .AddControllers(options => options.UseDateOnlyTimeOnlyStringConverters())
    .AddJsonOptions(options => options.UseDateOnlyTimeOnlyStringConverters());

builder.Services.AddSwaggerGen(c => c.UseDateOnlyTimeOnlyStringConverters());

builder.Services
    .AddHealthChecks();

var app = builder.Build();

app.MapControllers();
app.MapSwagger();
app.UseSwaggerUI();

app.MapHealthChecks("/healthz");

app.Run();
