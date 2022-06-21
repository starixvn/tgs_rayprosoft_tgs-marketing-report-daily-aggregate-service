var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("/config/appsettings.json", true);

builder.Services
    .AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/healthz");

app.Run();
