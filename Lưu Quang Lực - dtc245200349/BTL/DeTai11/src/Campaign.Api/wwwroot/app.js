"use strict";
const $ = id => document.getElementById(id);
const state = { selected: null, page: 1, campaignPage: 1, filter: "", busy: false };
const campaignPageSize = 10;
const deliveryPageSize = 10;
let listRequest = 0;
let detailRequest = 0;
let campaignSnapshot = "";
let deliverySnapshot = "";
const labels = { Draft: "Bản nháp", Running: "Đang gửi", Completed: "Hoàn tất", CompletedWithErrors: "Hoàn tất có lỗi", Pending: "Đang chờ", Processing: "Đang xử lý", RetryScheduled: "Chờ thử lại", Sent: "Đã gửi", Failed: "Thất bại", Interrupted: "Bị gián đoạn" };
const date = value => value ? new Date(value).toLocaleString("vi-VN") : "—";
function node(tag, text, className) { const el = document.createElement(tag); if (text !== undefined) el.textContent = text; if (className) el.className = className; return el; }
function notice(message, error = false) { $("notice").textContent = message; $("notice").className = error ? "error" : ""; $("notice").hidden = false; }
async function api(path, options = {}) {
  const response = await fetch(path, { ...options, headers: { "Content-Type": "application/json", "X-NotifyLab-Request": "1" }, signal: AbortSignal.timeout(15000) });
  if (response.status === 401) {
    location.replace("/login.html");
    throw new Error("Vui lòng đăng nhập lại.");
  }
  if (!response.ok) {
    const data = await response.json().catch(() => ({}));
    throw new Error(data.errors ? Object.values(data.errors).flat().join(" ") : data.error || data.title || `Lỗi HTTP ${response.status}`);
  }
  return response.status === 204 ? null : response.json();
}
function badge(status) { return node("span", labels[status] || status, "badge " + status); }
async function refresh() {
  const request = ++listRequest;
  const page = state.campaignPage;
  try {
    const campaigns = await api(`/campaigns?page=${page}&pageSize=${campaignPageSize}`);
    if (request !== listRequest || state.selected) return;
    const snapshot = JSON.stringify(campaigns);
    if (snapshot !== campaignSnapshot) {
      renderCampaigns(campaigns);
      campaignSnapshot = snapshot;
    }
    $("campaign-page").textContent = `Trang ${page}`;
    $("campaign-prev").disabled = page <= 1;
    $("campaign-next").disabled = campaigns.length < campaignPageSize;
  } catch (error) {
    if (request === listRequest && !state.selected) showError(error);
  }
}

function emptyRow(message) {
  const row = node("tr");
  const cell = node("td", message, "empty");
  cell.colSpan = 5;
  row.append(cell);
  return row;
}

function renderCampaigns(campaigns) {
    const list = $("campaign-list"); list.replaceChildren();
    if (!campaigns.length) list.append(emptyRow("Chưa có chiến dịch ở trang này."));
    for (const campaign of campaigns) {
      const row = node("tr");
      const title = node("td", undefined, "campaign-name");
      const button = node("button", campaign.name, "text-button");
      button.id = `campaign-${campaign.id}`;
      button.onclick = () => openCampaign(campaign.id);
      title.append(button, node("span", campaign.subject, "subject"));
      const status = node("td");
      status.append(badge(campaign.status));
      row.append(title, status, node("td", String(campaign.total), "numeric"),
        node("td", String(campaign.sent), "numeric"), node("td", date(campaign.createdAt), "date"));
      list.append(row);
    }
}

async function openCampaign(id) {
  state.selected = id;
  state.page = 1;
  state.filter = "";
  $("filter").value = "";
  deliverySnapshot = "";
  try {
    await loadDetails();
    if (state.selected === id) $("detail-name").focus();
  } catch (error) {
    if (state.selected !== id) return;
    state.selected = null;
    showError(error);
  }
}

async function loadDetails() {
  const selected = state.selected, page = state.page, filter = state.filter;
  if (!selected) return;
  const request = ++detailRequest;
  const [campaign, deliveries] = await Promise.all([
    api(`/campaigns/${selected}`),
    api(`/campaigns/${selected}/deliveries?page=${page}&pageSize=10&status=${filter}`)
  ]);
  if (request !== detailRequest || selected !== state.selected || page !== state.page || filter !== state.filter) return;
  $("campaigns").hidden = true;
  $("details").hidden = false; $("detail-name").textContent = campaign.name;
  $("detail-status").replaceChildren(badge(campaign.status));
  $("detail-subject").textContent = campaign.subject;
  for (const key of ["total", "pending", "sent", "failed"]) $(key).textContent = campaign[key];
  $("progress").value = campaign.total ? (campaign.sent + campaign.failed) / campaign.total * 100 : 0;
  $("start").disabled = state.busy || campaign.status !== "Draft";
  $("retry").disabled = state.busy || campaign.failed === 0;
  $("start").hidden = campaign.status !== "Draft";
  $("retry").hidden = campaign.status === "Draft";
  $("page-label").textContent = `Trang ${page}`;
  $("prev").disabled = page <= 1;
  $("next").disabled = deliveries.length < deliveryPageSize;
  const snapshot = JSON.stringify(deliveries);
  if (snapshot === deliverySnapshot) return;
  deliverySnapshot = snapshot;
  const tbody = $("delivery-list"); tbody.replaceChildren();
  for (const delivery of deliveries) {
    const row = node("tr"); row.append(node("td", delivery.address));
    const status = node("td"); status.append(badge(delivery.status)); row.append(status);
    row.append(node("td", String(delivery.totalAttempts), "numeric"), node("td", delivery.lastError || date(delivery.sentAt)));
    const cell = node("td"), button = node("button", "Xem", "text-button");
    button.setAttribute("aria-label", `Xem lịch sử gửi tới ${delivery.address}`);
    button.onclick = () => showHistory(selected, delivery).catch(showError);
    cell.append(button); row.append(cell); tbody.append(row);
  }
  if (!deliveries.length) tbody.append(emptyRow("Không có kết quả phù hợp."));
}
function showError(error) {
  const message = error.name === "TimeoutError" ? "API phản hồi quá lâu. Vui lòng thử lại." : error.message;
  if ($("create-dialog").open) {
    $("create-error").textContent = message;
    $("create-error").hidden = false;
    $("create-error").scrollIntoView({ block: "nearest" });
  } else {
    notice(message, true);
  }
}
async function showHistory(campaign, delivery) {
  const attempts = await api(`/campaigns/${campaign}/deliveries/${delivery.id}/attempts`);
  if (state.selected !== campaign) return;
  $("history-address").textContent = delivery.address;
  const content = $("history-content");
  content.replaceChildren();
  if (!attempts.length) content.append(node("p", "Chưa có lần gửi nào."));
  for (const attempt of attempts) {
    const item = node("div", undefined, "attempt"); item.append(badge(attempt.outcome), node("p", `Phiên ${attempt.generation} · Worker ${attempt.workerId}`), node("p", `${date(attempt.startedAt)} → ${date(attempt.finishedAt)}`));
    if (attempt.error) item.append(node("p", attempt.error)); content.append(item);
  }
  if (!$("history").open) $("history").showModal();
}
$("create-form").onsubmit = async event => {
  event.preventDefault(); $("create-button").disabled = true;
  $("create-error").hidden = true;
  try {
    const recipients = $("recipients").value.split(/[\n,;]+/).map(value => value.trim()).filter(Boolean);
    const result = await api("/campaigns", { method: "POST", body: JSON.stringify({ name: $("name").value, subject: $("subject").value, body: $("body").value, recipients }) });
    $("create-dialog").close();
    $("create-form").reset();
    state.campaignPage = 1;
    notice(`Đã tạo bản nháp với ${result.recipientCount} người nhận.`);
    await openCampaign(result.id);
  } catch (error) { showError(error); } finally { $("create-button").disabled = false; }
};
for (const [button, action, message] of [["start", "start", "Đã tiếp nhận yêu cầu gửi."], ["retry", "retry-failed", "Đã đưa các job thất bại vào lần gửi mới."]]) {
  $(button).onclick = async () => {
    if (state.busy || !state.selected) return;
    state.busy = true; $("start").disabled = true; $("retry").disabled = true;
    try { await api(`/campaigns/${state.selected}/${action}`, { method: "POST" }); notice(message); }
    catch (error) { showError(error); }
    finally { state.busy = false; await loadDetails().catch(showError); }
  };
}
$("sample").onclick = () => { $("recipients").value = "an@example.test\nbinh@example.test\nretry+demo@example.test\nfail+demo@example.test"; };
$("import-file").onchange = async event => {
  try { const file = event.target.files[0]; if (!file) return; if (file.size > 1024 * 1024) throw new Error("Tệp vượt quá 1 MB."); $("recipients").value = await file.text(); }
  catch (error) { showError(error); }
};
$("filter").onchange = () => { state.filter = $("filter").value; state.page = 1; loadDetails().catch(showError); };
$("prev").onclick = () => { state.page = Math.max(1, state.page - 1); loadDetails().catch(showError); };
$("next").onclick = () => { state.page++; loadDetails().catch(showError); };
$("campaign-prev").onclick = () => { state.campaignPage = Math.max(1, state.campaignPage - 1); refresh(); };
$("campaign-next").onclick = () => { state.campaignPage++; refresh(); };
$("refresh").onclick = refresh;
$("logout").onclick = async () => {
  try {
    await api("/auth/logout", { method: "POST" });
    location.replace("/login.html");
  } catch (error) {
    showError(error);
  }
};
$("close-history").onclick = () => $("history").close();
$("open-create").onclick = () => {
  $("create-error").hidden = true;
  $("create-dialog").showModal();
};
for (const id of ["cancel-create", "close-create"]) {
  $(id).onclick = () => $("create-dialog").close();
}
$("back").onclick = async () => {
  const id = state.selected;
  state.selected = null;
  $("details").hidden = true;
  $("campaigns").hidden = false;
  $("notice").hidden = true;
  await refresh();
  if (!state.selected) ($( `campaign-${id}`) || $("open-create")).focus();
};
let auditPage = 1;
let auditRequest = 0;
async function loadAudit(page) {
  const id = state.selected;
  const request = ++auditRequest;
  $("audit-status").textContent = "Đang tải nhật ký…";
  $("audit-prev").disabled = true;
  $("audit-next").disabled = true;
  try {
    const events = await api(`/campaigns/${id}/audit?page=${page}&pageSize=20`);
    if (request !== auditRequest || !$("audit").open) return;
    const rows = events.map(item => {
      const row = node("tr");
      const outcome = node("td");
      outcome.append(badge(item.outcome));
      row.append(node("td", date(item.occurredAt), "date"), outcome,
        node("td", item.deliveryId), node("td", String(item.generation)));
      return row;
    });
    $("audit-events").replaceChildren(...rows);
    $("audit-table").hidden = !rows.length;
    $("audit-status").textContent = rows.length ? "" : "Chưa có sự kiện ở trang này.";
    auditPage = page;
    $("audit-page").textContent = `Trang ${page}`;
    $("audit-prev").disabled = page <= 1;
    $("audit-next").disabled = rows.length < 20;
  } catch (error) {
    if (request === auditRequest && $("audit").open) {
      $("audit-table").hidden = true;
      $("audit-status").textContent = error.message;
    }
  }
}
$("show-audit").onclick = () => {
  $("audit-campaign").textContent = $("detail-name").textContent;
  $("audit-table").hidden = true;
  auditPage = 1;
  $("audit-page").textContent = "Trang 1";
  $("audit").showModal();
  loadAudit(auditPage);
};
$("close-audit").onclick = () => $("audit").close();
$("audit-prev").onclick = () => loadAudit(auditPage - 1);
$("audit-next").onclick = () => loadAudit(auditPage + 1);
$("audit-refresh").onclick = () => loadAudit(auditPage);

async function start() {
  const info = await api("/api/info");
  $("logout").hidden = false;
  if (info.externalSmtp) {
    $("mailpit-link").hidden = true;
    $("smtp-mode").hidden = false;
    $("sample").hidden = true;
    $("recipients").placeholder = "Mỗi dòng một địa chỉ email";
  }
  if (info.demoFailures) {
    $("demo-note").textContent = "Ví dụ kiểm thử: retry+… thành công ở lần 3, fail+… luôn thất bại.";
    $("demo-note").hidden = false;
  }
  poll();
}

async function poll() {
  try {
    if (state.selected) await loadDetails();
    else await refresh();
  } catch (error) {
    showError(error);
  } finally {
    setTimeout(poll, 3000);
  }
}
start().catch(showError);
