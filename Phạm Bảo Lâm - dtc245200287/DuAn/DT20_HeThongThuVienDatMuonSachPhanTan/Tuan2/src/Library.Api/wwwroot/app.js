const booksTableBody = document.getElementById("booksTableBody");
const reservationsTableBody = document.getElementById("reservationsTableBody");
const keywordInput = document.getElementById("keywordInput");
const reservationForm = document.getElementById("reservationForm");
const formFeedback = document.getElementById("formFeedback");

function setFeedback(message, isError = false) {
  formFeedback.textContent = message;
  formFeedback.className = `feedback ${isError ? "feedback--err" : "feedback--ok"}`;
}

async function loadBooks(keyword = "") {
  const query = keyword ? `?keyword=${encodeURIComponent(keyword)}` : "";
  const res = await fetch(`/books${query}`);
  if (!res.ok) throw new Error(`Lỗi tải danh sách sách (${res.status})`);
  const books = await res.json();

  document.getElementById("statBooks").textContent = books.length;
  const totalAvail = books.reduce((sum, b) => sum + (b.availableCopies ?? b.AvailableCopies ?? 0), 0);
  document.getElementById("statAvailable").textContent = totalAvail;

  if (!books.length) {
    booksTableBody.innerHTML = `<tr><td colspan="5" class="muted">Không tìm thấy sách phù hợp.</td></tr>`;
    return;
  }

  booksTableBody.innerHTML = books.map((b) => {
    const id = b.id ?? b.Id;
    const title = b.title ?? b.Title ?? "";
    const author = b.author ?? b.Author ?? "";
    const avail = b.availableCopies ?? b.AvailableCopies ?? 0;
    const total = b.totalCopies ?? b.TotalCopies ?? 0;
    return `
      <tr>
        <td>#${id}</td>
        <td><strong>${title}</strong></td>
        <td>${author}</td>
        <td>${avail} / ${total}</td>
        <td>
          <button class="btn btn--secondary btn--sm js-select-book" type="button"
                  data-book-id="${id}" data-book-title="${title}" ${avail <= 0 ? "disabled" : ""}>
            ${avail <= 0 ? "Hết sách" : "Chọn đặt"}
          </button>
        </td>
      </tr>`;
  }).join("");
}

booksTableBody.addEventListener("click", (e) => {
  const btn = e.target.closest(".js-select-book");
  if (!btn || btn.disabled) return;
  const bookId = btn.dataset.bookId;
  const bookTitle = btn.dataset.bookTitle;
  const input = document.getElementById("bookIdInput");
  input.value = bookId;
  input.classList.add("input-flash");
  setTimeout(() => input.classList.remove("input-flash"), 900);
  setFeedback(`Đã chọn sách #${bookId} – "${bookTitle}". Bấm "Gửi yêu cầu đặt mượn" để xác nhận.`);
  reservationForm.scrollIntoView({ behavior: "smooth", block: "nearest" });
});

async function loadReservations() {
  const userId = Number(document.getElementById("filterUserInput").value);
  if (!userId || userId <= 0) return;

  const status = document.getElementById("filterStatusSelect").value;
  const query = status ? `?status=${encodeURIComponent(status)}` : "";
  const res = await fetch(`/users/${userId}/reservations${query}`);

  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    reservationsTableBody.innerHTML = `<tr><td colspan="5" class="muted">${err.error || `Không tải được lịch sử (${res.status})`}</td></tr>`;
    document.getElementById("statReservations").textContent = "0";
    return;
  }

  const items = await res.json();
  document.getElementById("statReservations").textContent = items.length;
  renderReservations(items);
}

function renderReservations(items) {
  if (!items.length) {
    reservationsTableBody.innerHTML = `<tr><td colspan="5" class="muted">Người dùng này chưa có phiếu đặt mượn nào.</td></tr>`;
    return;
  }

  reservationsTableBody.innerHTML = items.map((r) => {
    const id = r.id ?? r.Id;
    const shortId = String(id).slice(0, 8);
    const bookId = r.bookId ?? r.BookId ?? "";
    const bookTitle = r.bookTitle ?? r.BookTitle ?? `Sách #${bookId}`;
    const status = r.status ?? r.Status ?? "Pending";
    const reason = r.reason ?? r.Reason ?? "-";

    let actionHtml = `<span class="muted">—</span>`;
    if (status === "Reserved") {
      actionHtml = `<button class="btn btn--primary btn--sm js-res-action" type="button" data-id="${id}" data-action="borrow">Cho mượn</button>`;
    } else if (status === "Borrowed") {
      actionHtml = `<button class="btn btn--secondary btn--sm js-res-action" type="button" data-id="${id}" data-action="return">Nhận trả</button>`;
    }

    return `
      <tr>
        <td><code title="${id}">${shortId}…</code></td>
        <td><strong>${bookTitle}</strong> (#${bookId})</td>
        <td><span class="badge status--${status}">${status}</span></td>
        <td>${reason}</td>
        <td>${actionHtml}</td>
      </tr>`;
  }).join("");
}

reservationsTableBody.addEventListener("click", async (e) => {
  const btn = e.target.closest(".js-res-action");
  if (!btn) return;
  const { id, action } = btn.dataset;
  try {
    const res = await fetch(`/reservations/${id}/${action}`, { method: "POST" });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) throw new Error(data.error || `Thao tác ${action} thất bại (${res.status})`);
    setFeedback(action === "borrow" ? "Đã xác nhận cho mượn sách (Borrowed)." : "Đã nhận trả sách và cộng lại bản khả dụng (Returned).");
    await Promise.all([loadBooks(keywordInput.value.trim()), loadReservations()]);
  } catch (err) {
    setFeedback(err.message, true);
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
    document.getElementById("filterUserInput").value = userId;
    setFeedback(`Đã tạo phiếu ${(data.id ?? data.Id ?? "").slice(0, 8)}… (Pending) – Worker đang xử lý...`);
    await Promise.all([loadBooks(keywordInput.value.trim()), loadReservations()]);
  } catch (err) {
    setFeedback(err.message, true);
  }
});

document.getElementById("btnSearch").addEventListener("click", () => loadBooks(keywordInput.value.trim()));
document.getElementById("btnResetBooks").addEventListener("click", () => { keywordInput.value = ""; loadBooks(""); });
document.getElementById("btnLoadReservations").addEventListener("click", loadReservations);
document.getElementById("filterStatusSelect").addEventListener("change", loadReservations);

async function init() {
  try {
    await Promise.all([loadBooks(""), loadReservations()]);
  } catch (err) {
    setFeedback(err.message, true);
  }
  setInterval(() => {
    loadBooks(keywordInput.value.trim()).catch(() => {});
    loadReservations().catch(() => {});
  }, 5000);
}

init();
