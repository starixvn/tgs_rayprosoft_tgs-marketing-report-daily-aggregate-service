using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tgs.MarketingReport.DailyAggregate.Client;

internal class MarketingReportDailyAggregateClient : IMarketingReportDailyAggregateClient
{
    private static readonly JsonSerializerOptions _SerializerOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _httpClient;

    public MarketingReportDailyAggregateClient(
        HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async ValueTask GenerateMarkingDailyAggregateReportAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            StartDate = startDate.ToString("yyyy-MM-dd"),
            EndDate = endDate.ToString("yyyy-MM-dd")
        };

        var requestContent = new StringContent(
            JsonSerializer.Serialize(request, _SerializerOptions),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var response = await _httpClient
            .PutAsync(
                $"/marketingreport/generate",
                requestContent,
                cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();
    }
}
