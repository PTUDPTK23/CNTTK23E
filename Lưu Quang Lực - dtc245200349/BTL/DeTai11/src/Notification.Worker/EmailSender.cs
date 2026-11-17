using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Configuration;
using Notification.Shared;

namespace Notification.Worker;

public sealed record SendResult(string? Error = null, bool Permanent = false);

public sealed class EmailSender(IConfiguration config)
{
    public async Task<SendResult> SendAsync(ClaimedDelivery delivery, CancellationToken ct)
    {
        // Chỉ có hiệu lực khi bật cờ demo. Không phụ thuộc dịch vụ gửi email trả phí.
        if (config.GetValue("Delivery:EnableDemoFailures", false))
        {
            if (delivery.Address.StartsWith("fail+", StringComparison.OrdinalIgnoreCase))
                return new("Demo: lỗi vĩnh viễn được cấu hình bằng tiền tố fail+.", true);
            if (delivery.Address.StartsWith("retry+", StringComparison.OrdinalIgnoreCase) && delivery.AttemptCount < 3)
                return new("Demo: lỗi tạm thời, sẽ thành công ở lần thử thứ ba.");
        }
        using var smtp = new SmtpClient(config["Smtp:Host"] ?? "localhost", config.GetValue("Smtp:Port", 1025))
        {
            EnableSsl = config.GetValue("Smtp:EnableSsl", false),
            UseDefaultCredentials = false
        };
        if (!string.IsNullOrEmpty(config["Smtp:Username"]))
            smtp.Credentials = new NetworkCredential(config["Smtp:Username"], config["Smtp:Password"]);
        using var mail = new MailMessage(config["Smtp:From"] ?? "notifications@example.test", delivery.Address)
        {
            Subject = delivery.Subject, Body = delivery.Body, IsBodyHtml = false,
            SubjectEncoding = Encoding.UTF8, BodyEncoding = Encoding.UTF8
        };
        mail.Headers.Add("X-Campaign-Id", delivery.CampaignId.ToString());
        mail.Headers.Add("X-Delivery-Id", delivery.Id.ToString());
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            await smtp.SendMailAsync(mail, timeout.Token);
            return new();
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new("SMTP timeout; kết quả phía SMTP có thể chưa xác định.");
        }
        catch (SmtpException ex)
        {
            var permanent = (int)ex.StatusCode is >= 500 and <= 599;
            // Không lưu ex.Message vì nhà cung cấp có thể chèn địa chỉ/ngữ liệu nhạy cảm.
            return new($"SMTP error: {ex.StatusCode}", permanent);
        }
    }
}
