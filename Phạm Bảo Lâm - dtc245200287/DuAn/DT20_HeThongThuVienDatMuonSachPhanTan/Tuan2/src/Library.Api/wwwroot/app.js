const ACCOUNTS = {
  docgia: {
    username: "docgia",
    password: "123456",
    role: "reader",
    label: "Độc giả (docgia • User #1)",
    defaultUserId: 1
  },
  thuthu: {
    username: "thuthu",
    password: "123456",
    role: "librarian",
    label: "Thủ thư (thuthu)",
    defaultUserId: 1
  }
};

const authSection = document.getElementById("authSection");
const appWorkspace = document.getElementById("appWorkspace");
const readerView = document.getElementById("readerView");
const librarianView = document.getElementById("librarianView");
const loginForm = document.getElementById("loginForm");
const loginUsername = document.getElementById("loginUsername");
const loginPassword = document.getElementById("loginPassword");
const loginFeedback = document.getElementById("loginFeedback");
const sessionInfo = document.getElementById("sessionInfo");
const btnLogout = document.getElementById("btnLogout");

const bookCardsGrid = document.getElementById("bookCardsGrid");
const myReservationsList = document.getElementById("myReservationsList");
const libReservationsBody = document.getElementById("libReservationsBody");
const libBooksBody = document.getElementById("libBooksBody");
const keywordInput = document.getElementById("keywordInput");
const reservationForm = document.getElementById("reservationForm");
const formFeedback = document.getElementById("formFeedback");
const libFeedback = document.getElementById("libFeedback");
const bookDetailBox = document.getElementById("bookDetailBox");
const lookupResultBox = document.getElementById("lookupResultBox");
const healthBadge = document.getElementById("healthBadge");

let currentAccount = null;
let activeUserId = 1;

function statusPillClass(status) {
  switch (status) {
    case "Reserved": return "pill--green";
    case "Borrowed": return "pill--blue";
    case "Rejected": return "pill--red";
    case "Returned": return "pill--slate";
    default: return "pill--amber";
  }
}

function setMsg(el, text, isError = false) {
  el.textContent = text;
  el.className = `feedback ${isError ? "feedback--err" : "feedback--ok"}`;
}

function applySessionUI() {
  const loggedIn = Boolean(currentAccount);
  authSection.classList.toggle("hidden", loggedIn);
  appWorkspace.classList.toggle("hidden", !loggedIn);
  btnLogout.classList.toggle("hidden", !loggedIn);

  if (!loggedIn) {
    sessionInfo.textContent = "Chưa đăng nhập";
    readerView.classList.add("hidden");
    librarianView.classList.add("hidden");
    return;
  }

  sessionInfo.innerHTML = `Đang đăng nhập: <strong>${currentAccount.label}</strong>`;
  const isReader = currentAccount.role === "reader";
  readerView.classList.toggle("hidden", !isReader);
  librarianView.classList.toggle("hidden", isReader);
}

function tryLogin(username, password) {
  const key = String(username || "").trim().toLowerCase();
  const acc = ACCOUNTS[key];
  if (!acc || acc.password !== password) {
    setMsg(loginFeedback, "Sai tên đăng nhập hoặc mật khẩu. Dùng 'docgia' / '123456' hoặc 'thuthu' / '123456'.", true);
    return;
  }
  currentAccount = acc;
  setMsg(loginFeedback, "");
  syncUserInputs(acc.defaultUserId);
  applySessionUI();
  refreshAll().catch(() => {});
}

loginForm.addEventListener("submit", (e) => {
  e.preventDefault();
  tryLogin(loginUsername.value, loginPassword.value);
});

authSection.addEventListener("click", (e) => {
  const fillBtn = e.target.closest(".js-fill-cred");
  if (fillBtn) {
    loginUsername.value = fillBtn.dataset.user;
    loginPassword.value = fillBtn.dataset.pass;
    setMsg(loginFeedback, `Đã điền '${fillBtn.dataset.user}'. Bấm "Đăng nhập vào hệ thống" để tiếp tục.`);
    return;
  }
  const quickBtn = e.target.closest(".js-quick-login");
  if (quickBtn) {
    loginUsername.value = quickBtn.dataset.user;
    loginPassword.value = quickBtn.dataset.pass;
    tryLogin(quickBtn.dataset.user, quickBtn.dataset.pass);
  }
});

btnLogout.addEventListener("click", () => {
  currentAccount = null;
  loginUsername.value = "";
  loginPassword.value = "";
  applySessionUI();
});

function syncUserInputs(uid) {
  activeUserId = uid;
  document.getElementById("userIdInput").value = uid;
  document.getElementById("filterUserInput").value = uid;
  document.getElementById("libUserInput").value = uid;
}

async function checkHealth() {
  try {
    const res = await fetch("/health");
    if (res.ok) {
      healthBadge.textContent = "● API & DB Healthy";
      healthBadge.className = "pill pill--green";
    } else {
      healthBadge.textContent = `● Cảnh báo (${res.status})`;
      healthBadge.className = "pill pill--red";
    }
  } catch {
    healthBadge.textContent = "● Mất kết nối";
    healthBadge.className = "pill pill--red";
  }
}

async function loadBooks(keyword = "") {
  const q = keyword ? `?keyword=${encodeURIComponent(keyword)}` : "";
  const res = await fetch(`/books${q}`);
  if (!res.ok) throw new Error(`Lỗi tải sách (${res.status})`);
  const books = await res.json();

  const totalAvail = books.reduce((s, b) => s + (b.availableCopies ?? b.AvailableCopies ?? 0), 0);
  document.getElementById("readerBookCount").textContent = `${books.length} đầu sách`;
  document.getElementById("kpiBooks").textContent = books.length;
  document.getElementById("kpiAvailable").textContent = totalAvail;

  renderReaderBookCards(books);
  renderLibrarianBooksTable(books);
}

function renderReaderBookCards(books) {
  if (!books.length) {
    bookCardsGrid.innerHTML = `<p class="empty-msg">Không tìm thấy sách phù hợp.</p>`;
    return;
  }
  bookCardsGrid.innerHTML = books.map((b, idx) => {
    const id = b.id ?? b.Id;
    const title = b.title ?? b.Title ?? "";
    const author = b.author ?? b.Author ?? "";
    const avail = b.availableCopies ?? b.AvailableCopies ?? 0;
    const total = b.totalCopies ?? b.TotalCopies ?? 0;
    const bannerClass = `book-banner--${idx % 4}`;
    return `
      <article class="book-item">
        <div>
          <div class="book-banner ${bannerClass}">
            <span>MÃ SÁCH #${id}</span>
            <span>${avail > 0 ? "CÓ SẴN" : "HẾT BẢN"}</span>
          </div>
          <div class="book-body">
            <h3 class="book-title">${title}</h3>
            <p class="book-author">Tác giả: ${author}</p>
          </div>
        </div>
        <div class="book-foot">
          <span class="stock-label">Khả dụng: <strong>${avail}</strong> / ${total} bản</span>
          <div class="btn-group">
            <button class="btn btn--outline btn--sm js-detail-book" type="button" data-book-id="${id}">Chi tiết</button>
            <button class="btn btn--primary btn--sm js-select-book" type="button"
                    data-book-id="${id}" data-book-title="${title}" ${avail <= 0 ? "disabled" : ""}>
              ${avail <= 0 ? "Hết sách" : "Đặt mượn"}
            </button>
          </div>
        </div>
      </article>`;
  }).join("");
}

function renderLibrarianBooksTable(books) {
  if (!books.length) {
    libBooksBody.innerHTML = `<tr><td colspan="5" class="empty-msg">Không có đầu sách nào.</td></tr>`;
    return;
  }
  libBooksBody.innerHTML = books.map((b) => {
    const id = b.id ?? b.Id;
    const title = b.title ?? b.Title ?? "";
    const author = b.author ?? b.Author ?? "";
    const avail = b.availableCopies ?? b.AvailableCopies ?? 0;
    const total = b.totalCopies ?? b.TotalCopies ?? 0;
    return `
      <tr>
        <td><code>#${id}</code></td>
        <td><strong>${title}</strong></td>
        <td>${author}</td>
        <td><strong>${avail}</strong> / ${total}</td>
        <td class="col-action">
          <span class="pill ${avail > 0 ? "pill--green" : "pill--red"}">${avail > 0 ? "Khả dụng" : "Hết bản"}</span>
        </td>
      </tr>`;
  }).join("");
}

async function showBookDetail(bookId) {
  const res = await fetch(`/books/${bookId}`);
  const data = await res.json().catch(() => ({}));
  bookDetailBox.classList.remove("hidden");
  if (!res.ok) {
    bookDetailBox.innerHTML = `<span class="feedback--err">${data.error || `Không tìm thấy sách #${bookId}`}</span>`;
    return;
  }
  const avail = data.availableCopies ?? data.AvailableCopies ?? 0;
  const total = data.totalCopies ?? data.TotalCopies ?? 0;
  bookDetailBox.innerHTML = `
    <strong>Chi tiết sách #${data.id ?? data.Id}: ${data.title ?? data.Title}</strong> •
    Tác giả: <span>${data.author ?? data.Author}</span> •
    Số bản khả dụng: <strong>${avail} / ${total}</strong>
    <span class="pill ${avail > 0 ? "pill--green" : "pill--red"}" style="margin-left:6px">${avail > 0 ? "Có thể đặt" : "Hết bản"}</span>`;
}

bookCardsGrid.addEventListener("click", async (e) => {
  const detailBtn = e.target.closest(".js-detail-book");
  if (detailBtn) {
    await showBookDetail(detailBtn.dataset.bookId);
    return;
  }
  const selectBtn = e.target.closest(".js-select-book");
  if (!selectBtn || selectBtn.disabled) return;
  const input = document.getElementById("bookIdInput");
  input.value = selectBtn.dataset.bookId;
  input.focus();
  setMsg(formFeedback, `Đã chọn sách #${selectBtn.dataset.bookId} ("${selectBtn.dataset.bookTitle}"). Nhấn "Gửi yêu cầu đặt mượn".`);
});

async function loadReservations() {
  const uid = Number(document.getElementById("filterUserInput").value) || activeUserId;
  const status = document.getElementById("filterStatusSelect").value;
  const query = status ? `?status=${encodeURIComponent(status)}` : "";
  const res = await fetch(`/users/${uid}/reservations${query}`);

  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    document.getElementById("kpiReservations").textContent = "0";
    myReservationsList.innerHTML = `<p class="empty-msg">${err.error || `Không tải được yêu cầu (${res.status})`}</p>`;
    libReservationsBody.innerHTML = `<tr><td colspan="5" class="empty-msg">${err.error || `Không tải được phiếu (${res.status})`}</td></tr>`;
    return;
  }

  const items = await res.json();
  document.getElementById("kpiReservations").textContent = items.length;
  renderReaderReservations(items);
  renderLibrarianReservations(items);
}

function renderReaderReservations(items) {
  if (!items.length) {
    myReservationsList.innerHTML = `<p class="empty-msg">Chưa có yêu cầu đặt mượn nào.</p>`;
    return;
  }
  myReservationsList.innerHTML = items.map((r) => {
    const id = r.id ?? r.Id;
    const shortId = String(id).slice(0, 8);
    const title = r.bookTitle ?? r.BookTitle ?? `Sách #${r.bookId ?? r.BookId}`;
    const status = r.status ?? r.Status ?? "Pending";
    const reason = r.reason ?? r.Reason;
    return `
      <div class="ticket-card">
        <div class="ticket-card__head">
          <strong>${title}</strong>
          <span class="pill ${statusPillClass(status)}">${status}</span>
        </div>
        <div class="ticket-card__meta">
          Mã: <code style="cursor:pointer" title="Copy mã phiếu" onclick="navigator.clipboard?.writeText('${id}')">${shortId}…</code>
          ${reason ? ` • ${reason}` : ""}
        </div>
      </div>`;
  }).join("");
}

function renderLibrarianReservations(items) {
  if (!items.length) {
    libReservationsBody.innerHTML = `<tr><td colspan="5" class="empty-msg">Không có phiếu nào.</td></tr>`;
    return;
  }
  libReservationsBody.innerHTML = items.map((r) => {
    const id = r.id ?? r.Id;
    const shortId = String(id).slice(0, 8);
    const title = r.bookTitle ?? r.BookTitle ?? `Sách #${r.bookId ?? r.BookId}`;
    const status = r.status ?? r.Status ?? "Pending";
    const reason = r.reason ?? r.Reason ?? "—";
    let actionHtml = `<span class="empty-msg">—</span>`;
    if (status === "Reserved") {
      actionHtml = `<button class="btn btn--primary btn--sm js-lib-action" type="button" data-id="${id}" data-action="borrow">Cho mượn</button>`;
    } else if (status === "Borrowed") {
      actionHtml = `<button class="btn btn--outline btn--sm js-lib-action" type="button" data-id="${id}" data-action="return">Nhận trả</button>`;
    }
    return `
      <tr>
        <td><code title="${id}">${shortId}…</code></td>
        <td><strong>${title}</strong></td>
        <td><span class="pill ${statusPillClass(status)}">${status}</span></td>
        <td>${reason}</td>
        <td class="col-action">${actionHtml}</td>
      </tr>`;
  }).join("");
}

libReservationsBody.addEventListener("click", async (e) => {
  const btn = e.target.closest(".js-lib-action");
  if (!btn) return;
  const { id, action } = btn.dataset;
  try {
    const res = await fetch(`/reservations/${id}/${action}`, { method: "POST" });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) throw new Error(data.error || `Thao tác thất bại (${res.status})`);
    setMsg(libFeedback, action === "borrow" ? "Đã xác nhận cho mượn sách (Borrowed)." : "Đã nhận trả sách (Returned) và cộng lại số bản khả dụng.");
    await refreshAll();
  } catch (err) {
    setMsg(libFeedback, err.message, true);
  }
});

reservationForm.addEventListener("submit", async (e) => {
  e.preventDefault();
  const userId = Number(document.getElementById("userIdInput").value);
  const bookId = Number(document.getElementById("bookIdInput").value);
  try {
    const res = await fetch("/reservations", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ userId, bookId })
    });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) {
      const detail = data.error || (data.errors ? Object.values(data.errors).flat().join("; ") : null);
      throw new Error(detail || `Không thể tạo phiếu (${res.status})`);
    }
    syncUserInputs(userId);
    const createdId = data.id ?? data.Id ?? "";
    const corrId = res.headers.get("X-Correlation-Id") || createdId;
    document.getElementById("lookupIdInput").value = createdId;
    setMsg(formFeedback, `Đã gửi vào RabbitMQ • Phiếu ${String(createdId).slice(0, 8)}… • Corr: ${String(corrId).slice(0, 10)}…`);
    await refreshAll();
  } catch (err) {
    setMsg(formFeedback, err.message, true);
  }
});

document.getElementById("btnLookupId").addEventListener("click", async () => {
  const id = document.getElementById("lookupIdInput").value.trim();
  if (!id) return;
  const res = await fetch(`/reservations/${encodeURIComponent(id)}`);
  const data = await res.json().catch(() => ({}));
  lookupResultBox.classList.remove("hidden");
  if (!res.ok) {
    lookupResultBox.innerHTML = `<span class="feedback--err">${data.error || `Không tìm thấy phiếu (${res.status})`}</span>`;
    return;
  }
  const st = data.status ?? data.Status;
  lookupResultBox.innerHTML = `
    <div><strong>${data.bookTitle ?? data.BookTitle ?? `Sách #${data.bookId ?? data.BookId}`}</strong> • User #${data.userId ?? data.UserId}</div>
    <div style="margin-top:4px">Trạng thái: <span class="pill ${statusPillClass(st)}">${st}</span> ${data.reason ? `• ${data.reason}` : ""}</div>`;
});

async function refreshAll() {
  await Promise.all([loadBooks(keywordInput.value.trim()), loadReservations()]);
}

document.getElementById("btnSearch").addEventListener("click", () => loadBooks(keywordInput.value.trim()));
document.getElementById("btnResetBooks").addEventListener("click", () => { keywordInput.value = ""; loadBooks(""); });
document.getElementById("btnLoadReservations").addEventListener("click", () => {
  syncUserInputs(Number(document.getElementById("filterUserInput").value) || 1);
  loadReservations();
});
document.getElementById("btnLibRefresh").addEventListener("click", () => {
  syncUserInputs(Number(document.getElementById("libUserInput").value) || 1);
  loadReservations();
});
document.getElementById("filterStatusSelect").addEventListener("change", loadReservations);

async function init() {
  applySessionUI();
  await checkHealth();
  setInterval(() => {
    checkHealth();
    if (currentAccount) {
      refreshAll().catch(() => {});
    }
  }, 5000);
}

init();
