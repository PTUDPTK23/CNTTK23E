using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Contracts.Data;
using Shared.Contracts.Enums;
using Shared.Contracts.Models;

namespace Notification.Worker;

public class NotificationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationWorker> _logger;
    private readonly WorkerSettings _settings;
    private readonly Random _random = new();

    public NotificationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationWorker> logger,
        IOptions<WorkerSettings> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("=================================================");
        _logger.LogInformation("🚀 Notification.Worker Service đã khởi chạy.");
        _logger.LogInformation("📦 BatchSize: {BatchSize} | Chu kỳ kiểm tra: {Interval}s | Thử lại tối đa: {MaxRetries}",
            _settings.BatchSize, _settings.PollIntervalSeconds, _settings.MaxRetries);
        _logger.LogInformation("=================================================");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingDeliveriesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "⚠️ Đã xảy ra lỗi trong chu kỳ xử lý thông báo.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_settings.PollIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("🛑 Notification.Worker Service đang dừng lại...");
    }

    private async Task ProcessPendingDeliveriesAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        // Kiểm tra khả năng kết nối cơ sở dữ liệu
        if (!await db.Database.CanConnectAsync(stoppingToken))
        {
            _logger.LogWarning("⏳ Chưa thể kết nối tới cơ sở dữ liệu PostgreSQL. Sẽ thử lại ở chu kỳ tiếp theo...");
            return;
        }

        // Lấy danh sách lượt gửi cần xử lý:
        // 1. Status == Pending
        // 2. Hoặc Status == Failed nhưng RetryCount < MaxRetries
        // 3. Campaign đang ở trạng thái Processing hoặc Pending
        var pendingJobs = await db.NotificationDeliveries
            .Include(d => d.Campaign)
            .Include(d => d.Recipient)
            .Where(d =>
                (d.Status == DeliveryStatus.Pending || (d.Status == DeliveryStatus.Failed && d.RetryCount < _settings.MaxRetries))
                && d.Campaign != null
                && (d.Campaign.Status == CampaignStatus.Processing || d.Campaign.Status == CampaignStatus.Pending))
            .OrderBy(d => d.CreatedAt)
            .Take(_settings.BatchSize)
            .ToListAsync(stoppingToken);

        if (pendingJobs.Count == 0)
        {
            return;
        }

        _logger.LogInformation("📬 Nhận được {Count} tác vụ gửi thông báo từ hàng đợi...", pendingJobs.Count);

        var affectedCampaignIds = new HashSet<Guid>();

        foreach (var job in pendingJobs)
        {
            if (stoppingToken.IsCancellationRequested) break;

            affectedCampaignIds.Add(job.CampaignId);

            var recipientName = job.Recipient?.FullName ?? "N/A";
            var destination = job.Recipient?.Email ?? job.Recipient?.PhoneNumber ?? "N/A";
            var title = job.Campaign?.Title ?? "N/A";
            var campaignName = job.Campaign?.Name ?? "N/A";

            _logger.LogInformation("➡️ [JobId: {JobId}] Đang gửi tin: \"{Title}\" -> [{RecipientName} ({Destination})] (Chiến dịch: \"{CampaignName}\")",
                job.JobId, title, recipientName, destination, campaignName);

            // Giả lập thời gian xử lý gửi qua mạng / gateway
            if (_settings.SimulationDelayMs > 0)
            {
                await Task.Delay(_settings.SimulationDelayMs, stoppingToken);
            }

            // Giả lập tỉ lệ gửi thành công / thất bại
            var isFailure = _random.Next(100) < _settings.SimulateFailureRatePercent;

            if (!isFailure)
            {
                // Gửi thành công
                job.Status = DeliveryStatus.Sent;
                job.ProcessedAt = DateTime.UtcNow;
                job.ErrorMessage = null;

                _logger.LogInformation("✅ [JobId: {JobId}] GỬI THÀNH CÔNG đến {RecipientName} ({Destination})",
                    job.JobId, recipientName, destination);
            }
            else
            {
                // Giả lập gửi thất bại và xử lý thử lại (Retry)
                job.RetryCount++;
                job.ProcessedAt = DateTime.UtcNow;

                if (job.RetryCount >= _settings.MaxRetries)
                {
                    job.Status = DeliveryStatus.Failed;
                    job.ErrorMessage = $"Gửi thất bại sau {job.RetryCount} lần thử: Cổng gửi tin SMS/Email không phản hồi hoặc địa chỉ không hợp lệ.";

                    _logger.LogWarning("❌ [JobId: {JobId}] GỬI THẤT BẠI CHÍNH THỨC đến {RecipientName} (Đã thử {RetryCount}/{MaxRetries} lần). Lỗi: {Error}",
                        job.JobId, recipientName, job.RetryCount, _settings.MaxRetries, job.ErrorMessage);
                }
                else
                {
                    job.Status = DeliveryStatus.Pending; // Tiếp tục xếp hàng chờ thử lại
                    job.ErrorMessage = $"Lần thử {job.RetryCount}/{_settings.MaxRetries} thất bại vào lúc {DateTime.UtcNow:HH:mm:ss}: Kết nối mạng gián đoạn, tự động xếp hàng thử lại.";

                    _logger.LogWarning("⚠️ [JobId: {JobId}] Lần gửi {RetryCount}/{MaxRetries} không thành công. Xếp hàng lại để gửi lại...",
                        job.JobId, job.RetryCount, _settings.MaxRetries);
                }
            }
        }

        // Lưu cập nhật trạng thái các lượt gửi
        await db.SaveChangesAsync(stoppingToken);

        // Kiểm tra xem các chiến dịch đã hoàn thành toàn bộ hay chưa
        foreach (var campaignId in affectedCampaignIds)
        {
            var remainingPending = await db.NotificationDeliveries
                .AnyAsync(d => d.CampaignId == campaignId && d.Status == DeliveryStatus.Pending, stoppingToken);

            if (!remainingPending)
            {
                var campaign = await db.Campaigns.FindAsync([campaignId], stoppingToken);
                if (campaign != null && campaign.Status != CampaignStatus.Completed)
                {
                    campaign.Status = CampaignStatus.Completed;
                    campaign.CompletedAt = DateTime.UtcNow;
                    _logger.LogInformation("🎉 Chiến dịch [{CampaignId}] \"{Name}\" ĐÃ HOÀN TẤT toàn bộ quá trình gửi!",
                        campaign.Id, campaign.Name);
                }
            }
        }

        await db.SaveChangesAsync(stoppingToken);
    }
}
