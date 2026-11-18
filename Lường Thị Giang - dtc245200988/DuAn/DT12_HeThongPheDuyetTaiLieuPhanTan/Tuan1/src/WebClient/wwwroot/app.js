const API_BASE = window.APP_CONFIG?.documentApiUrl || "http://localhost:5001";
const NOTIF_BASE =
  window.APP_CONFIG?.notificationUrl || "http://localhost:5002";

let allDocuments = [];
let notifications = [];
let readNotifKeys = new Set();
let selectedRecipient = "";
let sampleIndex = 0;

const SAMPLE_DOCS = [
  {
    title: "Đề xuất mua sắm thiết bị phòng Lab CNTT",
    category: "Đề xuất",
    content:
      "Trang bị bổ sung 02 máy chủ thực hành Docker và hệ thống mạng nội bộ phục vụ môn Phát triển ứng dụng phân tán.",
  },
  {
    title: "Kế hoạch nghiệm thu bài tập lớn Đề tài 12",
    category: "Kế hoạch",
    content:
      "Kiểm thử tích hợp luồng xử lý bất đồng bộ qua RabbitMQ, lưu trữ PostgreSQL và đẩy thông báo thời gian thực qua SignalR.",
  },
  {
    title: "Hợp đồng thuê hạ tầng Cloud triển khai Microservices",
    category: "Hợp đồng",
    content:
      "Cung cấp cụm máy chủ ảo hóa triển khai các dịch vụ DocumentService, NotificationService và WebClient.",
  },
];

function fillSampleData() {
  const sample = SAMPLE_DOCS[sampleIndex % SAMPLE_DOCS.length];
  sampleIndex++;

  document.getElementById("title").value = sample.title;
  document.getElementById("category").value = sample.category;
  document.getElementById("content").value = sample.content;

  const creatorInput = document.getElementById("creator");
  const approverInput = document.getElementById("approver");
  if (!creatorInput.value.trim()) creatorInput.value = "Lường Thị Giang";
  if (!approverInput.value.trim()) approverInput.value = "Giảng viên hướng dẫn";
}

function updateStats(docs) {
  document.getElementById("statTotal").textContent = docs.length;
  document.getElementById("statDraft").textContent = docs.filter(
    (d) => d.status === "Draft",
  ).length;
  document.getElementById("statSubmitted").textContent = docs.filter(
    (d) => d.status === "Submitted",
  ).length;
  document.getElementById("statApproved").textContent = docs.filter(
    (d) => d.status === "Approved",
  ).length;
  document.getElementById("statRejected").textContent = docs.filter(
    (d) => d.status === "Rejected",
  ).length;
}

async function loadDocuments() {
  const listEl = document.getElementById("docList");
  try {
    const res = await fetch(`${API_BASE}/documents`);
    if (!res.ok) throw new Error("API error");
    allDocuments = await res.json();
    updateStats(allDocuments);
    renderRecipientChips();
    applyClientFilters();
  } catch {
    listEl.innerHTML = `<div class="empty-dashed" style="color:#dc2626; border-color:#fecaca; background:#fef2f2;">Không thể kết nối Document API (${API_BASE}). Hãy kiểm tra container đang chạy.</div>`;
  }
}

function applyClientFilters() {
  const status = document.getElementById("filterStatus").value;
  const approverQuery = document
    .getElementById("filterApprover")
    .value.trim()
    .toLowerCase();
  const creatorQuery = document
    .getElementById("filterCreator")
    .value.trim()
    .toLowerCase();

  const filtered = allDocuments.filter((doc) => {
    if (status && doc.status !== status) return false;
    if (
      approverQuery &&
      !(doc.approver || "").toLowerCase().includes(approverQuery)
    )
      return false;
    if (
      creatorQuery &&
      !(doc.creator || "").toLowerCase().includes(creatorQuery)
    )
      return false;
    return true;
  });

  renderDocuments(filtered);
}

function resetFilters() {
  document.getElementById("filterStatus").value = "";
  document.getElementById("filterApprover").value = "";
  document.getElementById("filterCreator").value = "";
  applyClientFilters();
}

function renderDocuments(docs) {
  const listEl = document.getElementById("docList");

  if (!docs.length) {
    listEl.innerHTML = `<div class="empty-dashed">Chưa có tài liệu nào phù hợp bộ lọc. Bấm <strong>✨ Điền mẫu</strong> ở cột bên trái để tạo nhanh!</div>`;
    return;
  }

  listEl.innerHTML = docs
    .map(
      (doc) => `
    <div class="doc-item">
      <div class="doc-top">
        <div class="doc-title">${escapeHtml(doc.title)}</div>
        <span class="badge badge-${doc.status}">${doc.status}</span>
      </div>
      <div class="doc-meta">
        <span>Loại: <strong>${escapeHtml(doc.category)}</strong></span>
        <span>Người tạo: <strong>${escapeHtml(doc.creator)}</strong></span>
        <span>Người duyệt: <strong>${escapeHtml(doc.approver)}</strong></span>
      </div>
      ${doc.content ? `<div class="doc-summary">${escapeHtml(doc.content)}</div>` : ""}
      ${doc.rejectionReason ? `<div style="color:#b91c1c; font-size:12px; margin-bottom:10px; background:#fef2f2; padding:6px 10px; border-radius:8px; border:1px solid #fecaca;"><strong>Lý do từ chối:</strong> ${escapeHtml(doc.rejectionReason)}</div>` : ""}
      <div class="doc-actions">
        ${
          doc.status === "Draft" || doc.status === "Rejected"
            ? `<button type="button" class="btn btn-primary btn-sm" onclick="submitDoc('${doc.id}', '${escapeAttr(doc.creator)}')">Gửi phê duyệt</button>`
            : ""
        }
        ${
          doc.status === "Submitted"
            ? `<button type="button" class="btn btn-success btn-sm" onclick="approveDoc('${doc.id}', '${escapeAttr(doc.approver)}')">Phê duyệt</button>
             <button type="button" class="btn btn-danger btn-sm" onclick="rejectDoc('${doc.id}', '${escapeAttr(doc.approver)}')">Từ chối</button>`
            : ""
        }
        <button type="button" class="btn btn-outline btn-sm" onclick="viewHistory('${doc.id}')">Lịch sử (${doc.histories?.length || 0})</button>
      </div>
    </div>
  `,
    )
    .join("");
}

document.getElementById("createForm").addEventListener("submit", async (e) => {
  e.preventDefault();
  const payload = {
    title: document.getElementById("title").value,
    category: document.getElementById("category").value,
    creator: document.getElementById("creator").value,
    approver: document.getElementById("approver").value,
    content: document.getElementById("content").value,
  };

  const res = await fetch(`${API_BASE}/documents`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (res.ok) {
    document.getElementById("title").value = "";
    document.getElementById("content").value = "";
    await loadDocuments();
  } else {
    const err = await res.json();
    alert(err.message || "Lỗi tạo tài liệu");
  }
});

async function submitDoc(id, creator) {
  const comment = prompt(
    "Nhập ghi chú gửi phê duyệt:",
    "Kính trình phê duyệt hồ sơ.",
  );
  if (comment === null) return;

  const res = await fetch(`${API_BASE}/documents/${id}/submit`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ actor: creator, comment }),
  });

  if (!res.ok) {
    const err = await res.json();
    alert(err.message || "Lỗi gửi duyệt");
  }
  await loadDocuments();
}

async function approveDoc(id, approver) {
  const comment = prompt(
    "Nhập ghi chú phê duyệt:",
    "Đồng ý phê duyệt theo đề xuất.",
  );
  if (comment === null) return;

  const res = await fetch(`${API_BASE}/documents/${id}/approve`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ actor: approver, comment }),
  });

  if (!res.ok) {
    const err = await res.json();
    alert(err.message || "Lỗi phê duyệt");
  }
  await loadDocuments();
}

async function rejectDoc(id, approver) {
  const reason = prompt(
    "Nhập lý do từ chối tài liệu:",
    "Cần bổ sung thêm số liệu chi tiết.",
  );
  if (reason === null) return;

  const res = await fetch(`${API_BASE}/documents/${id}/reject`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ actor: approver, reason }),
  });

  if (!res.ok) {
    const err = await res.json();
    alert(err.message || "Lỗi từ chối");
  }
  await loadDocuments();
}

async function viewHistory(id) {
  const res = await fetch(`${API_BASE}/documents/${id}`);
  if (!res.ok) return;
  const doc = await res.json();

  document.getElementById("modalTitle").textContent = doc.title;
  document.getElementById("modalBody").innerHTML = `
    <div style="display:grid; gap:6px; font-size:13.5px; margin-bottom:16px;">
      <div><strong>Mã hồ sơ:</strong> <code style="background:#f1f5f9; padding:2px 6px; border-radius:4px;">${doc.id}</code></div>
      <div><strong>Người tạo:</strong> ${escapeHtml(doc.creator)} &nbsp;|&nbsp; <strong>Người duyệt:</strong> ${escapeHtml(doc.approver)}</div>
      <div><strong>Trạng thái:</strong> <span class="badge badge-${doc.status}">${doc.status}</span></div>
      <div><strong>Nội dung:</strong> ${escapeHtml(doc.content || "(Không có)")}</div>
    </div>
    <h4 style="margin:0 0 12px; font-size:14px; color:#0f172a;">Lịch sử các bước xử lý</h4>
    <div style="border-left:2px solid #cbd5e1; margin-left:6px; padding-left:16px; display:flex; flex-direction:column; gap:14px;">
      ${(doc.histories || [])
        .map(
          (h) => `
        <div style="position:relative;">
          <span style="position:absolute; left:-22px; top:4px; width:10px; height:10px; border-radius:50%; background:#2f65f6;"></span>
          <div style="font-size:13.5px;"><strong>${escapeHtml(h.action)}</strong> (${escapeHtml(h.fromStatus)} &rarr; ${escapeHtml(h.toStatus)}) bởi <strong>${escapeHtml(h.performedBy)}</strong></div>
          ${h.comment ? `<div style="font-size:13px; color:#475569; margin-top:2px;">${escapeHtml(h.comment)}</div>` : ""}
          <div style="font-size:11.5px; color:#64748b; margin-top:3px;">${new Date(h.timestampUtc).toLocaleString("vi-VN")} &bull; CorrelationId: <code>${escapeHtml(h.correlationId)}</code></div>
        </div>
      `,
        )
        .join("")}
    </div>
  `;
  document.getElementById("detailModal").classList.add("open");
}

function closeModal() {
  document.getElementById("detailModal").classList.remove("open");
}

function getNotifKey(n) {
  return n.id || `${n.correlationId}_${n.createdAtUtc}_${n.newStatus}`;
}

function markAllNotifsRead() {
  notifications.forEach((n) => readNotifKeys.add(getNotifKey(n)));
  renderNotifications();
}

function renderRecipientChips() {
  const names = new Set(["Lường Thị Giang", "Giảng viên hướng dẫn"]);
  allDocuments.forEach((d) => {
    if (d.creator) names.add(d.creator);
    if (d.approver) names.add(d.approver);
  });

  const chipWrap = document.getElementById("notifQuickChips");
  const allChip = `<button type="button" class="chip ${selectedRecipient === "" ? "active" : ""}" onclick="selectRecipient('')">Tất cả</button>`;
  const personChips = Array.from(names)
    .map(
      (name) =>
        `<button type="button" class="chip ${selectedRecipient === name ? "active" : ""}" onclick="selectRecipient('${escapeAttr(name)}')">${escapeHtml(name)}</button>`,
    )
    .join("");

  chipWrap.innerHTML = allChip + personChips;
}

function selectRecipient(name) {
  selectedRecipient = name;
  document.getElementById("notifRecipientFilter").value = name;
  renderRecipientChips();
  renderNotifications();
}

function getNotifBorderColor(status) {
  if (status === "Approved") return "#16a34a";
  if (status === "Rejected") return "#dc2626";
  if (status === "Submitted") return "#d97706";
  return "#2f65f6";
}

function renderNotifications() {
  const unreadCount = notifications.filter(
    (n) => !readNotifKeys.has(getNotifKey(n)),
  ).length;
  document.getElementById("notifCount").textContent = `${unreadCount} mới`;

  const filterText = document
    .getElementById("notifRecipientFilter")
    .value.trim()
    .toLowerCase();
  const filtered = notifications.filter((n) => {
    if (!filterText) return true;
    const target =
      `${n.recipient || ""} ${n.headline || ""} ${n.detailMessage || ""}`.toLowerCase();
    return target.includes(filterText);
  });

  const container = document.getElementById("notifList");
  if (!filtered.length) {
    container.innerHTML = `<div class="empty-dashed">Chưa có thông báo sự kiện nào.</div>`;
    return;
  }

  container.innerHTML = filtered
    .map((n) => {
      const isUnread = !readNotifKeys.has(getNotifKey(n));
      const borderColor = getNotifBorderColor(n.newStatus);
      return `
      <div class="notif-item ${isUnread ? "unread" : ""}" style="border-left-color: ${borderColor};">
        <div class="notif-title">${escapeHtml(n.headline)}</div>
        <div class="notif-body">${escapeHtml(n.detailMessage)}</div>
        <div class="notif-foot">
          <span>${new Date(n.createdAtUtc).toLocaleTimeString("vi-VN")}</span>
          <span>CID: ${escapeHtml(n.correlationId)}</span>
        </div>
      </div>
    `;
    })
    .join("");
}

async function initNotifications() {
  try {
    const res = await fetch(`${NOTIF_BASE}/notifications`);
    if (res.ok) {
      notifications = await res.json();
      notifications.forEach((n) => readNotifKeys.add(getNotifKey(n)));
      renderNotifications();
    }
  } catch {}

  const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${NOTIF_BASE}/hubs/notifications`)
    .withAutomaticReconnect()
    .build();

  connection.on("ReceiveNotification", (notif) => {
    notifications.unshift(notif);
    renderNotifications();
    loadDocuments();
  });

  try {
    await connection.start();
  } catch {}
}

function escapeHtml(str) {
  return String(str ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");
}

function escapeAttr(str) {
  return String(str ?? "").replaceAll("'", "\\'");
}

renderRecipientChips();
loadDocuments();
initNotifications();
