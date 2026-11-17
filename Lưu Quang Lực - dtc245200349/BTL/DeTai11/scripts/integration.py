"""Integration tests using only Python's standard library. Run against a local demo stack."""
import argparse
import base64
import concurrent.futures
import http.cookiejar
import json
import os
from pathlib import Path
import subprocess
import time
import urllib.error
import urllib.request
import uuid

parser = argparse.ArgumentParser()
parser.add_argument("--url", default="http://localhost:5080")
parser.add_argument("--rabbit-url", default="http://localhost:15672")
parser.add_argument("--faults", action="store_true", help="Temporarily stop/start only this project's containers")
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
run_id = uuid.uuid4().hex[:8]
cookies = http.cookiejar.CookieJar()
session = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cookies))


def request(path, data=None, base=None, auth=False, anonymous=False, mutation_header=True):
    headers = {"Content-Type": "application/json"}
    if data is not None and mutation_header and not auth:
        headers["X-NotifyLab-Request"] = "1"
    if auth:
        password = os.environ.get("RABBITMQ_PASSWORD", "notifylab_local_demo")
        token = base64.b64encode(f"notifylab:{password}".encode()).decode()
        headers["Authorization"] = "Basic " + token
    req = urllib.request.Request((base or args.url) + path, headers=headers,
                                 data=None if data is None else json.dumps(data).encode())
    open_request = urllib.request.urlopen if auth or anonymous else session.open
    with open_request(req, timeout=20) as response:
        raw = response.read()
        return json.loads(raw) if raw else None


def check(condition, message):
    if not condition:
        raise AssertionError(message)
    print("PASS:", message, flush=True)


def expect_http(code, action, message):
    try:
        action()
    except urllib.error.HTTPError as error:
        check(error.code == code, f"{message} (expected {code}, got {error.code})")
    else:
        raise AssertionError(f"{message}: expected {code}")


def wait_for(function, timeout=120):
    deadline = time.monotonic() + timeout
    last_error = None
    while time.monotonic() < deadline:
        try:
            value = function()
            if value:
                return value
        except (urllib.error.URLError, TimeoutError, ConnectionError) as exc:
            last_error = exc
        time.sleep(1)
    raise TimeoutError(f"Condition not met after {timeout}s: {last_error}")


def create(recipients, suffix):
    result = request("/campaigns", {"name": f"Integration {run_id} {suffix}", "subject": "NotifyLab integration",
                                  "body": "Integration test mail; local Mailpit only.", "recipients": recipients})
    return result["id"]


def summary(campaign):
    result = request(f"/campaigns/{campaign}")
    assert result["total"] == result["pending"] + result["sent"] + result["failed"], "Counter invariant"
    return result


def completed(campaign):
    result = summary(campaign)
    return result if result["status"] in ("Completed", "CompletedWithErrors") else None


def compose(*arguments):
    subprocess.run(["docker", "compose", "-f", str(root / "docker-compose.yml"), *arguments],
                   cwd=root, check=True, stdout=subprocess.DEVNULL)


wait_for(lambda: request("/health/ready"))
for entry in ("/", "/index.html"):
    with urllib.request.urlopen(args.url + entry, timeout=20) as page:
        check(page.geturl().endswith("/login.html") and page.headers.get("Cache-Control") == "no-store"
              and b'id="login-form"' in page.read(), f"Guest navigation from {entry} reaches the login form")
expect_http(401, lambda: request("/campaigns", anonymous=True), "Anonymous users cannot list campaigns")
expect_http(401, lambda: request("/campaigns", {}, anonymous=True), "Anonymous users cannot create campaigns")
admin_user = os.environ.get("ADMIN_USERNAME", "admin")
admin_password = os.environ.get("ADMIN_PASSWORD", "notifylab_demo_admin")
expect_http(401, lambda: request("/auth/login", {"username": admin_user, "password": "incorrect"}),
            "Invalid password is rejected")
request("/auth/login", {"username": admin_user, "password": admin_password})
check(request("/auth/session")["username"] == admin_user, "Admin signs in with a session cookie")
with session.open(args.url + "/", timeout=20) as page:
    check(page.geturl().endswith("/") and b'id="campaign-list"' in page.read(), "Admin navigation loads the dashboard")
expect_http(403, lambda: request("/campaigns", {}, mutation_header=False), "Unsafe request requires the same-origin header")
info = request("/api/info")
check(info["demoFailures"] and not info["externalSmtp"], "Demo checks use Mailpit with deterministic failures")
try:
    request("/campaigns", {"name": "Invalid", "subject": "Bad", "body": "Body", "recipients": ["not-email"]})
    raise AssertionError("Invalid email accepted")
except urllib.error.HTTPError as error:
    check(error.code == 400, "Invalid email returns 400")

recipients = [f"user{i}.{run_id}@example.test" for i in range(30)]
recipients += [f"retry+{run_id}@example.test", f"fail+{run_id}@example.test", recipients[0].upper()]
campaign = create(recipients, "main")
check(summary(campaign)["total"] == 32, "Duplicate recipient removed")
with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
    list(pool.map(lambda _: request(f"/campaigns/{campaign}/start", {}), range(4)))
result = wait_for(lambda: completed(campaign))
check(result["total"] == 32 and result["sent"] == 31 and result["failed"] == 1,
      "Concurrent start is idempotent; 31 sent and 1 failed")
deliveries = request(f"/campaigns/{campaign}/deliveries?pageSize=100")
retry = next(item for item in deliveries if item["address"].startswith("retry+"))
failed = next(item for item in deliveries if item["address"].startswith("fail+"))
check(retry["totalAttempts"] == 3, "Temporary failure retries exactly three times")
check(failed["totalAttempts"] == 1, "Permanent failure does not auto-retry")
audit_path = f"/campaigns/{campaign}/audit?pageSize=100"
expect_http(401, lambda: request(audit_path, anonymous=True), "Audit data is restricted to logged-in admins")
events = wait_for(lambda: (items if len(items := request(audit_path)) >= 34 else None))
check(len(events) == 34 and sum(item["outcome"] == "Sent" for item in events) == 31 and
      sum(item["outcome"] == "Failed" for item in events) == 1 and
      sum(item["outcome"] == "RetryScheduled" for item in events) == 2,
      "Independent subscriber records sent, failed and retried attempts")
replayed = {key[0].upper() + key[1:]: value for key, value in events[0].items()}
replay = request("/api/exchanges/%2F/notifications.events/publish",
                 {"properties": {"delivery_mode": 2}, "routing_key": f"delivery.{events[0]['outcome'].lower()}",
                  "payload": json.dumps(replayed), "payload_encoding": "string"}, base=args.rabbit_url, auth=True)
check(replay["routed"], "Existing audit event was replayed to its subscriber")
time.sleep(2)
check(len(request(audit_path)) == 34, "Replayed audit event is recorded only once")
workers = set()
for item in deliveries:
    history = request(f"/campaigns/{campaign}/deliveries/{item['id']}/attempts")
    workers.update(attempt["workerId"] for attempt in history)
check(len(workers) >= 2, "At least two independent Workers process jobs")
retry_result = request(f"/campaigns/{campaign}/retry-failed", {})
check(retry_result["retried"] == 1, "Manual retry only queues failed delivery")
wait_for(lambda: completed(campaign))
deliveries_after = request(f"/campaigns/{campaign}/deliveries?pageSize=100")
before = {item["id"]: item for item in deliveries}
check(all(item["totalAttempts"] == before[item["id"]]["totalAttempts"] for item in deliveries_after if item["status"] == "Sent"),
      "Manual retry does not resend successful deliveries")
check(next(item for item in deliveries_after if item["id"] == failed["id"])["totalAttempts"] == 2,
      "Manual retry retains attempt history")

# Redeliver an already-sent job to verify the consumer's DB guard.
sent = next(item for item in deliveries if item["status"] == "Sent" and item["totalAttempts"] == 1)
envelope = {"MessageId": str(uuid.uuid4()), "CampaignId": campaign, "DeliveryId": sent["id"], "Generation": 1, "SchemaVersion": 1}
publish = lambda payload: request("/api/exchanges/%2F/notifications.commands/publish",
    {"properties": {"delivery_mode": 2}, "routing_key": "notification.send", "payload": payload, "payload_encoding": "string"},
    base=args.rabbit_url, auth=True)
check(publish(json.dumps(envelope))["routed"], "Duplicate sent job routed for redelivery test")
time.sleep(3)
history = request(f"/campaigns/{campaign}/deliveries/{sent['id']}/attempts")
check(len(history) == 1, "Duplicate sent job creates no new send attempt")
publish("invalid-json")
wait_for(lambda: request("/api/queues/%2F/notification.dead", base=args.rabbit_url, auth=True).get("messages", 0) > 0)
check(True, "Malformed message isolated in dead-letter queue")

if args.faults:
    try:
        compose("stop", "notification-audit")
        delayed_audit = create([f"audit.{run_id}@example.test"], "audit-offline")
        request(f"/campaigns/{delayed_audit}/start", {})
        check(wait_for(lambda: completed(delayed_audit))["sent"] == 1, "Email sending continues while Audit is stopped")
        check(not request(f"/campaigns/{delayed_audit}/audit"), "Audit view stays empty while its subscriber is stopped")
        wait_for(lambda: request("/api/queues/%2F/notification.audit", base=args.rabbit_url, auth=True).get("messages_ready", 0) >= 1)
        compose("start", "notification-audit")
        check(len(wait_for(lambda: request(f"/campaigns/{delayed_audit}/audit"))) == 1,
              "Audit subscriber catches up from its durable queue")
        compose("stop", "notification-worker")
        queued = create([f"queued{i}.{run_id}@example.test" for i in range(10)], "worker-offline")
        request(f"/campaigns/{queued}/start", {})
        wait_for(lambda: request("/api/queues/%2F/notification.jobs", base=args.rabbit_url, auth=True).get("messages_ready", 0) >= 10)
        check(summary(queued)["pending"] == 10, "Work remains pending while Workers are stopped")
        compose("restart", "rabbitmq")
        wait_for(lambda: request("/api/queues/%2F/notification.jobs", base=args.rabbit_url, auth=True).get("messages_ready", 0) >= 10)
        check(True, "Durable messages survive broker restart")
        compose("start", "notification-worker")
        check(wait_for(lambda: completed(queued))["sent"] == 10, "Workers drain retained jobs after restart")
        compose("stop", "rabbitmq")
        offline = create([f"offline{i}.{run_id}@example.test" for i in range(3)], "broker-offline")
        request(f"/campaigns/{offline}/start", {})
        time.sleep(2)
        check(summary(offline)["pending"] == 3, "API accepts and persists work while broker is offline")
        compose("start", "rabbitmq")
        check(wait_for(lambda: completed(offline))["sent"] == 3, "Outbox publishes after broker recovery")
        compose("restart", "postgres")
        wait_for(lambda: request("/health/ready"))
        check(summary(campaign)["sent"] == 31, "Campaign state survives PostgreSQL restart")
    finally:
        compose("start", "postgres", "rabbitmq", "notification-worker", "notification-audit")

request("/auth/logout", {})
expect_http(401, lambda: request("/auth/session"), "Sign-out removes the session")
print(f"ALL INTEGRATION CHECKS PASSED. Campaign: {campaign}", flush=True)
