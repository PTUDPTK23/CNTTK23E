using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using EventFlow.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EventFlow.WebClient.Services;

public class AuditApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<AuditApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public AuditApiClient(HttpClient http, IConfiguration config, ILogger<AuditApiClient> logger)
    {
        _http = http;
        var baseUrl = config["Services:AuditWorkerUrl"] ?? "http://localhost:5002";
        _http.BaseAddress = new Uri(baseUrl);
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<AuditLogDto>> GetAuditLogsAsync(string? eventType = null, string? search = null, int limit = 50)
    {
        try
        {
            var url = "/api/audit/logs";
            var query = new List<string> { $"limit={limit}" };
            if (!string.IsNullOrWhiteSpace(eventType)) query.Add($"eventType={Uri.EscapeDataString(eventType)}");
            if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
            url += "?" + string.Join("&", query);

            var res = await _http.GetFromJsonAsync<List<AuditLogDto>>(url, _jsonOptions);
            return res ?? new List<AuditLogDto>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch audit logs from AuditWorker");
            return new List<AuditLogDto>();
        }
    }
}
