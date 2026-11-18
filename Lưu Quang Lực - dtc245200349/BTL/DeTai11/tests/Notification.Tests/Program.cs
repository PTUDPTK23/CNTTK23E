using Notification.Shared;

var passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name);
    passed++;
}
var valid = new CreateCampaign("Demo", "Thông báo", "Nội dung", ["a@example.test", " A@example.test "]);
var normalized = CampaignRules.Validate(valid);
Check(normalized.Error is null && normalized.Campaign!.Recipients.Length == 1, "Normalize/deduplicate addresses");
Check(CampaignRules.Validate(valid with { Recipients = ["invalid"] }).Error is not null, "Reject invalid address");
Check(CampaignRules.Validate(valid with { Recipients = [] }).Error is not null, "Reject empty audience");
Check(CampaignRules.Validate(valid with { Recipients = new string[5001] }).Error is not null, "Bound campaign size");
Check(CampaignRules.Validate(valid with { Subject = "Subject\r\nBcc: other@example.test" }).Error is not null, "Reject header injection");
Check(CampaignRules.Validate(valid with { Body = new string('x', 20001) }).Error is not null, "Bound body size");
Check(CampaignRules.ShouldRetry(false, 1, 3) && CampaignRules.ShouldRetry(false, 2, 3), "Retry temporary error before limit");
Check(!CampaignRules.ShouldRetry(false, 3, 3), "Stop retry at limit");
Check(!CampaignRules.ShouldRetry(true, 1, 3), "Do not retry permanent error");
Check(CampaignRules.RetryDelay(1).TotalSeconds == 10 && CampaignRules.RetryDelay(2).TotalSeconds == 30, "Retry backoff");
Check(CampaignRules.IsPending("Processing") && CampaignRules.IsPending("RetryScheduled") && !CampaignRules.IsPending("Failed"), "Aggregate pending states");
Console.WriteLine($"{passed} checks passed.");
