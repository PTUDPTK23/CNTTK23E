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

public class EventApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<EventApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public EventApiClient(HttpClient http, IConfiguration config, ILogger<EventApiClient> logger)
    {
        _http = http;
        var baseUrl = config["Services:EventApiUrl"] ?? "http://localhost:5000";
        _http.BaseAddress = new Uri(baseUrl);
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public async Task<List<EventDto>> GetEventsAsync(string? search = null, EventStatus? status = null)
    {
        try
        {
            var url = "/events";
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(search)) queryParams.Add($"search={Uri.EscapeDataString(search)}");
            if (status.HasValue) queryParams.Add($"status={status.Value}");
            if (queryParams.Count > 0) url += "?" + string.Join("&", queryParams);

            var res = await _http.GetFromJsonAsync<List<EventDto>>(url, _jsonOptions);
            return res ?? new List<EventDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get events");
            return new List<EventDto>();
        }
    }

    public async Task<EventDto?> GetEventByIdAsync(Guid id)
    {
        try
        {
            return await _http.GetFromJsonAsync<EventDto>($"/events/{id}", _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get event {Id}", id);
            return null;
        }
    }

    public async Task<(bool Success, EventDto? Data, string? Error)> CreateEventAsync(CreateEventRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/events", request, _jsonOptions);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<EventDto>(_jsonOptions);
                return (true, data, null);
            }
            var errJson = await response.Content.ReadAsStringAsync();
            return (false, null, ParseErrorMessage(errJson, "Tạo sự kiện thất bại"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create event");
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Success, EventDto? Data, string? Error)> UpdateEventAsync(Guid id, UpdateEventRequest request)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"/events/{id}", request, _jsonOptions);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<EventDto>(_jsonOptions);
                return (true, data, null);
            }
            var errJson = await response.Content.ReadAsStringAsync();
            return (false, null, ParseErrorMessage(errJson, "Cập nhật sự kiện thất bại"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update event {Id}", id);
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Success, RegistrationDetailDto? Data, string? Error)> RegisterParticipantAsync(Guid eventId, RegisterParticipantRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"/events/{eventId}/registrations", request, _jsonOptions);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<RegistrationDetailDto>(_jsonOptions);
                return (true, data, null);
            }
            var errJson = await response.Content.ReadAsStringAsync();
            return (false, null, ParseErrorMessage(errJson, "Đăng ký không thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register participant for event {EventId}", eventId);
            return (false, null, "Không thể kết nối đến máy chủ: " + ex.Message);
        }
    }

    public async Task<(bool Success, string Message)> CancelRegistrationAsync(Guid registrationId)
    {
        try
        {
            var response = await _http.DeleteAsync($"/registrations/{registrationId}");
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return (true, ParseErrorMessage(content, "Hủy đăng ký thành công"));
            }
            return (false, ParseErrorMessage(content, "Hủy đăng ký thất bại"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel registration {Id}", registrationId);
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, CheckInResultDto? Data, string Message)> CheckInByCodeAsync(string code)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/checkin", new CheckInRequest { RegistrationCode = code }, _jsonOptions);
            var content = await response.Content.ReadFromJsonAsync<CheckInResultDto>(_jsonOptions);
            if (response.IsSuccessStatusCode && content != null)
            {
                return (true, content, content.Message);
            }
            if (content != null)
            {
                return (false, content, content.Message);
            }
            return (false, null, "Check-in không thành công. Vui lòng kiểm tra lại mã vé.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check in code {Code}", code);
            return (false, null, "Lỗi kết nối máy chủ: " + ex.Message);
        }
    }

    public async Task<(bool Success, CheckInResultDto? Data, string Message)> CheckInByIdAsync(Guid registrationId)
    {
        try
        {
            var response = await _http.PostAsync($"/registrations/{registrationId}/checkin", null);
            var content = await response.Content.ReadFromJsonAsync<CheckInResultDto>(_jsonOptions);
            if (response.IsSuccessStatusCode && content != null)
            {
                return (true, content, content.Message);
            }
            if (content != null)
            {
                return (false, content, content.Message);
            }
            return (false, null, "Check-in không thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check in registration {Id}", registrationId);
            return (false, null, "Lỗi kết nối máy chủ: " + ex.Message);
        }
    }

    public async Task<List<RegistrationDto>> GetRegistrationsByEventAsync(Guid eventId)
    {
        try
        {
            var res = await _http.GetFromJsonAsync<List<RegistrationDto>>($"/events/{eventId}/registrations", _jsonOptions);
            return res ?? new List<RegistrationDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get registrations for event {EventId}", eventId);
            return new List<RegistrationDto>();
        }
    }

    public async Task<List<RegistrationDto>> GetAllRegistrationsAsync(string? search = null, RegistrationStatus? status = null)
    {
        try
        {
            var url = "/registrations";
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
            if (status.HasValue) query.Add($"status={status.Value}");
            if (query.Count > 0) url += "?" + string.Join("&", query);

            var res = await _http.GetFromJsonAsync<List<RegistrationDto>>(url, _jsonOptions);
            return res ?? new List<RegistrationDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all registrations");
            return new List<RegistrationDto>();
        }
    }

    public async Task<RegistrationDetailDto?> GetRegistrationByCodeAsync(string code)
    {
        try
        {
            return await _http.GetFromJsonAsync<RegistrationDetailDto>($"/registrations/code/{Uri.EscapeDataString(code.Trim())}", _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Registration not found with code {Code}", code);
            return null;
        }
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        try
        {
            var res = await _http.GetFromJsonAsync<DashboardStatsDto>("/api/dashboard/stats", _jsonOptions);
            return res ?? new DashboardStatsDto();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch dashboard stats from EventApi");
            return new DashboardStatsDto();
        }
    }

    private static string ParseErrorMessage(string json, string defaultMsg)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("message", out var msgProp))
            {
                return msgProp.GetString() ?? defaultMsg;
            }
        }
        catch
        {
            // not valid JSON
        }
        return string.IsNullOrWhiteSpace(json) ? defaultMsg : json;
    }
}
