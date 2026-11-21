using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Shared.Contracts.DTOs;
using Shared.Contracts.Enums;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

// Đọc cấu hình từ appsettings.json hoặc biến môi trường
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

var apiBaseUrl = (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
    ? args[0].Trim()
    : (config["ApiSettings:BaseUrl"] 
       ?? Environment.GetEnvironmentVariable("ApiSettings__BaseUrl") 
       ?? "http://localhost:5000");

using var httpClient = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/"),
    Timeout = TimeSpan.FromSeconds(15)
};

var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
};

while (true)
{
    SafeClear();
    PrintBanner(apiBaseUrl);

    Console.WriteLine(" 1. 📋 Xem danh sách tất cả chiến dịch");
    Console.WriteLine(" 2. ➕ Tạo chiến dịch thông báo mới (Gửi hàng loạt)");
    Console.WriteLine(" 3. 🔍 Xem chi tiết chiến dịch & tiến độ gửi");
    Console.WriteLine(" 4. 👥 Xem danh sách người nhận");
    Console.WriteLine(" 5. ➕ Thêm người nhận mới");
    Console.WriteLine(" 6. 🚀 Kích hoạt phát chiến dịch (Send Campaign)");
    Console.WriteLine(" 7. 📊 Giám sát tiến độ theo thời gian thực (Live Monitor)");
    Console.WriteLine(" 8. 🩺 Kiểm tra tình trạng kết nối API & CSDL (Health Check)");
    Console.WriteLine(" 0. 🚪 Thoát");
    Console.WriteLine();
    Console.Write("👉 Chọn chức năng [0-8]: ");

    var rawInput = Console.ReadLine();
    if (rawInput == null) break; // Thoát nếu luồng nhập kết thúc (EOF)
    var choice = rawInput.Trim();
    Console.WriteLine();

    try
    {
        switch (choice)
        {
            case "1":
                await ListCampaignsAsync(httpClient, jsonOptions);
                break;
            case "2":
                await CreateCampaignAsync(httpClient, jsonOptions);
                break;
            case "3":
                await ViewCampaignDetailAsync(httpClient, jsonOptions);
                break;
            case "4":
                await ListRecipientsAsync(httpClient, jsonOptions);
                break;
            case "5":
                await CreateRecipientAsync(httpClient, jsonOptions);
                break;
            case "6":
                await SendCampaignAsync(httpClient, jsonOptions);
                break;
            case "7":
                await LiveMonitorAsync(httpClient, jsonOptions);
                break;
            case "8":
                await CheckHealthAsync(httpClient, jsonOptions);
                break;
            case "0":
                Console.WriteLine("Tạm biệt!");
                return;
            default:
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Lựa chọn không hợp lệ. Vui lòng thử lại.");
                Console.ResetColor();
                break;
        }
    }
    catch (HttpRequestException ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n❌ Không thể kết nối đến Campaign.API tại {apiBaseUrl}: {ex.Message}");
        Console.WriteLine("Vui lòng đảm bảo dịch vụ Campaign.API đã được khởi chạy.");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n❌ Đã xảy ra lỗi: {ex.Message}");
        Console.ResetColor();
    }

    if (!Console.IsInputRedirected)
    {
        Console.WriteLine("\nNhấn phím bất kỳ để tiếp tục...");
        try { Console.ReadKey(true); } catch { }
    }
}

static void PrintBanner(string apiUrl)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║        HỆ THỐNG GỬI THÔNG BÁO HÀNG LOẠT (BULK NOTIFICATION)      ║");
    Console.WriteLine("║               BẢNG ĐIỀU KHIỂN QUẢN TRỊ (ADMIN CLIENT)            ║");
    Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
    Console.ResetColor();
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($" 🌐 Kết nối tới API: {apiUrl}");
    Console.WriteLine(new string('─', 68));
    Console.ResetColor();
}

static async Task ListCampaignsAsync(HttpClient client, JsonSerializerOptions jsonOptions)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("── DANH SÁCH CHIẾN DỊCH THÔNG BÁO ──");
    Console.ResetColor();

    var response = await client.GetAsync("api/campaigns");
    response.EnsureSuccessStatusCode();

    var campaigns = await response.Content.ReadFromJsonAsync<List<CampaignSummaryDto>>(jsonOptions);

    if (campaigns is null || campaigns.Count == 0)
    {
        Console.WriteLine("Chưa có chiến dịch nào được tạo.");
        return;
    }

    Console.WriteLine("{0,-38} | {1,-20} | {2,-12} | {3,6} | {4,6} | {5,6} | {6,6}",
        "Mã chiến dịch (ID)", "Tên chiến dịch", "Trạng thái", "Tổng", "Sent", "Fail", "Pend");
    Console.WriteLine(new string('-', 106));

    foreach (var c in campaigns)
    {
        var statusColor = c.Status switch
        {
            CampaignStatus.Completed => ConsoleColor.Green,
            CampaignStatus.Processing => ConsoleColor.Blue,
            CampaignStatus.Failed => ConsoleColor.Red,
            _ => ConsoleColor.DarkYellow
        };

        Console.Write("{0,-38} | {1,-20} | ", c.Id, Truncate(c.Name, 20));
        Console.ForegroundColor = statusColor;
        Console.Write("{0,-12}", c.Status);
        Console.ResetColor();
        Console.WriteLine(" | {0,6} | {1,6} | {2,6} | {3,6}",
            c.TotalRecipients, c.SentCount, c.FailedCount, c.PendingCount);
    }
}

static async Task CreateCampaignAsync(HttpClient client, JsonSerializerOptions jsonOptions)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("── TẠO CHIẾN DỊCH THÔNG BÁO MỚI ──");
    Console.ResetColor();

    Console.Write("Nhập tên chiến dịch: ");
    var name = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(name))
    {
        Console.WriteLine("Tên chiến dịch không được để trống!");
        return;
    }

    Console.Write("Nhập tiêu đề thông báo: ");
    var title = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(title))
    {
        Console.WriteLine("Tiêu đề không được để trống!");
        return;
    }

    Console.Write("Nhập nội dung thông báo: ");
    var content = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(content))
    {
        Console.WriteLine("Nội dung không được để trống!");
        return;
    }

    Console.Write("Kích hoạt gửi ngay lập tức? (Y/n): ");
    var sendImmediatelyInput = Console.ReadLine()?.Trim();
    var sendImmediately = string.IsNullOrWhiteSpace(sendImmediatelyInput) || sendImmediatelyInput.Equals("y", StringComparison.OrdinalIgnoreCase);

    var request = new CreateCampaignRequest
    {
        Name = name,
        Title = title,
        Content = content,
        SendImmediately = sendImmediately
    };

    var response = await client.PostAsJsonAsync("api/campaigns", request);
    if (!response.IsSuccessStatusCode)
    {
        var err = await response.Content.ReadAsStringAsync();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Tạo thất bại: {err}");
        Console.ResetColor();
        return;
    }

    var result = await response.Content.ReadFromJsonAsync<JsonElement>(jsonOptions);
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"\n✅ Đã tạo chiến dịch thành công!");
    Console.WriteLine($"Mã chiến dịch: {result.GetProperty("id").GetString()}");
    Console.WriteLine($"Tổng số lượt gửi tạo ra: {result.GetProperty("totalJobs").GetInt32()}");
    Console.WriteLine($"Trạng thái: {result.GetProperty("status").GetString()}");
    Console.ResetColor();
}

static async Task ViewCampaignDetailAsync(HttpClient client, JsonSerializerOptions jsonOptions)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("── CHI TIẾT CHIẾN DỊCH & NHẬT KÝ GỬI TIN ──");
    Console.ResetColor();

    Console.Write("Nhập mã chiến dịch (Guid): ");
    var idInput = Console.ReadLine()?.Trim();
    if (!Guid.TryParse(idInput, out var id))
    {
        Console.WriteLine("Mã chiến dịch không hợp lệ.");
        return;
    }

    var response = await client.GetAsync($"api/campaigns/{id}");
    if (!response.IsSuccessStatusCode)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Không tìm thấy chiến dịch {id}");
        Console.ResetColor();
        return;
    }

    var detail = await response.Content.ReadFromJsonAsync<CampaignDetailDto>(jsonOptions);
    if (detail is null) return;

    Console.WriteLine($"\nChiến dịch : {detail.Name}");
    Console.WriteLine($"Tiêu đề    : {detail.Title}");
    Console.WriteLine($"Nội dung   : {detail.Content}");
    Console.WriteLine($"Trạng thái : {detail.Status}");
    Console.WriteLine($"Ngày tạo   : {detail.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");
    Console.WriteLine($"Tiến độ    : Đã gửi {detail.SentCount}/{detail.TotalRecipients} | Thất bại: {detail.FailedCount} | Đang chờ: {detail.PendingCount}");

    Console.WriteLine("\n── Chi tiết từng lượt gửi (NotificationDeliveries) ──");
    Console.WriteLine("{0,-38} | {1,-18} | {2,-20} | {3,-8} | {4,5} | {5}",
        "Mã Job", "Người nhận", "Đích đến", "Status", "Retry", "Lỗi (nếu có)");
    Console.WriteLine(new string('-', 115));

    foreach (var d in detail.Deliveries)
    {
        var statusColor = d.Status switch
        {
            DeliveryStatus.Sent => ConsoleColor.Green,
            DeliveryStatus.Failed => ConsoleColor.Red,
            _ => ConsoleColor.Yellow
        };

        Console.Write("{0,-38} | {1,-18} | {2,-20} | ",
            d.JobId, Truncate(d.RecipientName, 18), Truncate(d.Destination ?? "N/A", 20));
        Console.ForegroundColor = statusColor;
        Console.Write("{0,-8}", d.Status);
        Console.ResetColor();
        Console.WriteLine(" | {0,5} | {1}", d.RetryCount, d.ErrorMessage ?? "-");
    }
}

static async Task ListRecipientsAsync(HttpClient client, JsonSerializerOptions jsonOptions)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("── DANH SÁCH NGƯỜI NHẬN ──");
    Console.ResetColor();

    var response = await client.GetAsync("api/recipients");
    response.EnsureSuccessStatusCode();

    var recipients = await response.Content.ReadFromJsonAsync<List<RecipientDto>>(jsonOptions);
    if (recipients is null || recipients.Count == 0)
    {
        Console.WriteLine("Chưa có người nhận nào trong hệ thống.");
        return;
    }

    Console.WriteLine("{0,-38} | {1,-25} | {2,-25} | {3,-15}",
        "Mã người nhận (ID)", "Họ và tên", "Email", "Số điện thoại");
    Console.WriteLine(new string('-', 110));

    foreach (var r in recipients)
    {
        Console.WriteLine("{0,-38} | {1,-25} | {2,-25} | {3,-15}",
            r.Id, Truncate(r.FullName, 25), Truncate(r.Email ?? "N/A", 25), r.PhoneNumber ?? "N/A");
    }
}

static async Task CreateRecipientAsync(HttpClient client, JsonSerializerOptions jsonOptions)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("── THÊM NGƯỜI NHẬN MỚI ──");
    Console.ResetColor();

    Console.Write("Họ và tên: ");
    var fullName = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(fullName))
    {
        Console.WriteLine("Họ tên không được để trống!");
        return;
    }

    Console.Write("Email: ");
    var email = Console.ReadLine()?.Trim();

    Console.Write("Số điện thoại: ");
    var phone = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
    {
        Console.WriteLine("Phải nhập ít nhất Email hoặc Số điện thoại!");
        return;
    }

    var request = new CreateRecipientRequest
    {
        FullName = fullName,
        Email = string.IsNullOrWhiteSpace(email) ? null : email,
        PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone
    };

    var response = await client.PostAsJsonAsync("api/recipients", request);
    if (response.IsSuccessStatusCode)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✅ Thêm người nhận thành công!");
        Console.ResetColor();
    }
    else
    {
        var err = await response.Content.ReadAsStringAsync();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Lỗi: {err}");
        Console.ResetColor();
    }
}

static async Task SendCampaignAsync(HttpClient client, JsonSerializerOptions jsonOptions)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("── KÍCH HOẠT PHÁT CHIẾN DỊCH ──");
    Console.ResetColor();

    Console.Write("Nhập mã chiến dịch (Guid): ");
    var idInput = Console.ReadLine()?.Trim();
    if (!Guid.TryParse(idInput, out var id))
    {
        Console.WriteLine("Mã không hợp lệ.");
        return;
    }

    var response = await client.PostAsync($"api/campaigns/{id}/send", null);
    if (response.IsSuccessStatusCode)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✅ Đã kích hoạt chiến dịch! Worker sẽ bắt đầu gửi thông báo.");
        Console.ResetColor();
    }
    else
    {
        var err = await response.Content.ReadAsStringAsync();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Lỗi: {err}");
        Console.ResetColor();
    }
}

static async Task LiveMonitorAsync(HttpClient client, JsonSerializerOptions jsonOptions)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("── GIÁM SÁT TIẾN ĐỘ THEO THỜI GIAN THỰC ──");
    Console.ResetColor();

    if (Console.IsInputRedirected)
    {
        try
        {
            var response = await client.GetAsync("api/campaigns");
            if (response.IsSuccessStatusCode)
            {
                var campaigns = await response.Content.ReadFromJsonAsync<List<CampaignSummaryDto>>(jsonOptions);
                if (campaigns is not null && campaigns.Count > 0)
                {
                    foreach (var c in campaigns.Take(10))
                    {
                        var pct = c.TotalRecipients > 0 ? (c.SentCount + c.FailedCount) * 100 / c.TotalRecipients : 0;
                        Console.WriteLine($"Chiến dịch: \"{c.Name}\" | Trạng thái: {c.Status} | Tiến độ: {pct}% ({c.SentCount} Sent, {c.FailedCount} Failed, {c.PendingCount} Pending / {c.TotalRecipients} Tổng)");
                    }
                }
            }
        }
        catch { }
        return;
    }

    Console.WriteLine("(Nhấn phím bất kỳ để dừng)");

    while (!Console.KeyAvailable)
    {
        try
        {
            var response = await client.GetAsync("api/campaigns");
            if (response.IsSuccessStatusCode)
            {
                var campaigns = await response.Content.ReadFromJsonAsync<List<CampaignSummaryDto>>(jsonOptions);
                SafeClear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"⏱ Cập nhật lúc {DateTime.Now:HH:mm:ss} - Nhấn phím bất kỳ để quay lại menu chính");
                Console.ResetColor();
                Console.WriteLine(new string('=', 95));

                if (campaigns is not null && campaigns.Count > 0)
                {
                    foreach (var c in campaigns.Take(10))
                    {
                        var pct = c.TotalRecipients > 0 ? (c.SentCount + c.FailedCount) * 100 / c.TotalRecipients : 0;
                        Console.WriteLine($"Chiến dịch: \"{c.Name}\" | Trạng thái: {c.Status} | Tiến độ: {pct}% ({c.SentCount} Sent, {c.FailedCount} Failed, {c.PendingCount} Pending / {c.TotalRecipients} Tổng)");
                    }
                }
                else
                {
                    Console.WriteLine("Chưa có chiến dịch nào.");
                }
            }
        }
        catch
        {
            // Bỏ qua lỗi tạm thời khi ngắt kết nối
        }

        await Task.Delay(2000);
    }

    try { Console.ReadKey(true); } catch { }
}

static async Task CheckHealthAsync(HttpClient client, JsonSerializerOptions jsonOptions)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("── KIỂM TRA TÌNH TRẠNG KẾT NỐI HỆ THỐNG ──");
    Console.ResetColor();

    var response = await client.GetAsync("health");
    if (response.IsSuccessStatusCode)
    {
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(jsonOptions);
        var status = result.GetProperty("status").GetString();
        var connected = result.GetProperty("connected").GetBoolean();

        if (connected)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ Campaign.API: Hoạt động bình thường (Healthy)");
            Console.WriteLine("✅ PostgreSQL: Kết nối cơ sở dữ liệu THÀNH CÔNG!");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️ Campaign.API: Đang chạy");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ PostgreSQL: Không thể kết nối cơ sở dữ liệu ({result.GetProperty("error").GetString()})");
            Console.ResetColor();
        }
    }
}

static void SafeClear()
{
    try
    {
        if (!Console.IsOutputRedirected)
            Console.Clear();
    }
    catch { }
}

static string Truncate(string value, int maxLength)
{
    if (string.IsNullOrEmpty(value)) return string.Empty;
    return value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
}
