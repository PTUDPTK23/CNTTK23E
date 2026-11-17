document.addEventListener("DOMContentLoaded", async () => {
  const api = window.APP_CONFIG?.apiBaseUrl ?? `http://${window.location.hostname}:5000`;
  const $ = (selector, root = document) => root.querySelector(selector);
  const moduleView = $("#module-view");
  const overview = $("#overview-content");
  const heading = $(".page-heading h1");
  const esc = (value) => String(value ?? "").replace(/[&<>"']/g, (char) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[char]);
  const money = (amount) => new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND", maximumFractionDigits: 0 }).format(amount || 0);
  const date = (value) => new Intl.DateTimeFormat("vi-VN", { day: "numeric", month: "short", year: "numeric" }).format(new Date(value));
  const request = async (path, options = {}) => {
    const response = await fetch(`${api}/api/v1/admin${path}`, { credentials: "include", ...options, headers: { ...(options.body ? { "Content-Type": "application/json" } : {}), ...options.headers } });
    if (!response.ok) {
      const error = await response.json().catch(() => ({}));
      throw new Error(error.message || `Yêu cầu thất bại (${response.status})`);
    }
    return response.status === 204 ? null : response.json();
  };
  const showError = (error) => { moduleView.hidden = false; overview.hidden = true; moduleView.innerHTML = `<section class="panel module-placeholder"><h2>Có lỗi khi tải dữ liệu</h2><p>${esc(error.message)}</p><button class="primary-button" id="retry-module" type="button">Thử lại</button></section>`; $("#retry-module")?.addEventListener("click", () => openModule(currentModule)); };
  let currentModule = "overview";
  let username = "Quản trị viên";

  try {
    const sessionResponse = await fetch(`${api}/api/v1/auth/admin/session`, { credentials: "include" });
    if (sessionResponse.status === 401 || sessionResponse.status === 403) { window.location.replace("../index.html"); return; }
    if (!sessionResponse.ok) throw new Error("Không xác thực được phiên quản trị.");
    const session = await sessionResponse.json(); username = session.username || username;
    $("#admin-username").textContent = username; $("#welcome-name").textContent = username;
    $("#admin-avatar").textContent = username.slice(0, 1).toLocaleUpperCase("vi");
    $("#today-label").textContent = new Intl.DateTimeFormat("vi-VN", { weekday: "long", day: "numeric", month: "long", year: "numeric" }).format(new Date());
    await loadOverview();
    $("#admin-loading").hidden = true; $("#admin-content").hidden = false;
  } catch (error) { $("#admin-loading").textContent = `Không kết nối được backend hoặc cơ sở dữ liệu: ${error.message}`; return; }

  async function loadOverview() {
    try {
      const data = await request("/overview");
      const values = $$(".metric-card .metric-value", overview);
      values[0].textContent = money(data.revenue); values[1].textContent = data.invoiceCount.toLocaleString("vi-VN");
      values[2].textContent = data.customerCount.toLocaleString("vi-VN"); values[3].textContent = money(data.pendingAmount);
      const metricNotes = ["Đã thanh toán trong 30 ngày", "Tổng hóa đơn trong cơ sở dữ liệu", "Tài khoản khách hàng", "Hóa đơn chờ thanh toán"];
      $$(".metric-card", overview).forEach((card, index) => { card.querySelector(".metric-foot").textContent = metricNotes[index]; card.querySelector(".sparkline").hidden = true; });
      const report = await request("/reports");
      const monthlyValues = Array.from({ length: 12 }, (_, index) => report.monthly.find(item => item.month === index + 1 && item.year === new Date().getFullYear())?.revenue || 0);
      const maxRevenue = Math.max(...monthlyValues, 1);
      const points = monthlyValues.map((value, index) => `${(index * 700 / 11).toFixed(1)} ${(190 - value / maxRevenue * 160).toFixed(1)}`);
      const linePath = `M${points.join(" L")}`;
      $$(".y-axis span").forEach((label, index) => { const fraction = 1 - index / 4; label.textContent = fraction === 0 ? "₫0" : `₫${Math.round(maxRevenue * fraction / 1_000_000).toLocaleString("vi-VN")}tr`; });
      $(".current-line").setAttribute("d", linePath);
      $(".current-fill").setAttribute("d", `${linePath} L700 220 L0 220 Z`);
      $(".previous-line").setAttribute("d", "");
      $(".chart-point").setAttribute("cx", "700"); $(".chart-point").setAttribute("cy", String(190 - monthlyValues[11] / maxRevenue * 160));
      const total = data.paid + data.pending + data.overdue || 1;
      const paidPct = data.paid / total * 100; const pendingPct = data.pending / total * 100;
      $(".donut").style.background = `conic-gradient(#7568db 0 ${paidPct}%,#f1b46f ${paidPct}% ${paidPct + pendingPct}%,#ed8290 ${paidPct + pendingPct}% 100%)`;
      $(".donut-center strong").textContent = data.invoiceCount.toLocaleString("vi-VN");
      const statuses = $$(".status-legend > div");
      [[data.paid, paidPct], [data.pending, pendingPct], [data.overdue, data.overdue / total * 100]].forEach(([count, pct], index) => { statuses[index].querySelector("strong").innerHTML = `${count} <small>${pct.toFixed(1)}%</small>`; });
      const tbody = $(".invoices-panel tbody");
      tbody.innerHTML = data.recentInvoices.length ? data.recentInvoices.map((invoice) => `<tr><td><span class="invoice-id">#${esc(invoice.code)}</span></td><td><span class="customer-cell">${esc(invoice.customerName)}</span></td><td>${date(invoice.createdAt)}</td><td class="amount">${money(invoice.amount)}</td><td><span class="status-badge ${invoice.status === "Paid" ? "status-paid" : invoice.status === "Overdue" ? "status-overdue" : "status-pending"}"><i></i>${statusLabel(invoice.status)}</span></td><td></td></tr>`).join("") : `<tr><td colspan="6">Chưa có hóa đơn. Hãy vào Quản lý hóa đơn để tạo mới.</td></tr>`;
      $(".table-footer > span").innerHTML = `Hiển thị <strong>${data.recentInvoices.length}</strong> trong <strong>${data.invoiceCount}</strong> hóa đơn`;
      $(".nav-count").textContent = data.invoiceCount.toLocaleString("vi-VN");
      $(".pagination").hidden = true;
      $(".demo-notice").hidden = true; $(".page-bottom span:last-child").textContent = "Dữ liệu trực tiếp từ cơ sở dữ liệu";
    } catch (error) { showError(error); }
  }
  function $$(selector, root = document) { return [...root.querySelectorAll(selector)]; }
  function statusLabel(status) { return ({ Paid: "Đã thanh toán", Pending: "Chờ thanh toán", Overdue: "Quá hạn", Cancelled: "Đã hủy" })[status] || status; }
  function layout(title, description, actions = "") {
    heading.textContent = title; $(".breadcrumb strong").textContent = title; $(".heading-subtitle").textContent = description;
    overview.hidden = true; moduleView.hidden = false;
    moduleView.innerHTML = `<div class="module-toolbar">${actions}</div><div id="module-body"><p class="module-loading">Đang tải dữ liệu…</p></div>`;
  }
  async function openModule(key) {
    currentModule = key;
    if (key === "overview") { heading.textContent = "Tổng quan"; $(".breadcrumb strong").textContent = "Tổng quan"; $(".heading-subtitle").innerHTML = `Chào mừng trở lại, <strong id="welcome-name">${esc(username)}</strong>. Đây là tình hình hoạt động hôm nay.`; moduleView.hidden = true; overview.hidden = false; await loadOverview(); return; }
    if (key === "accounts") return loadAccounts();
    if (key === "permissions") return loadRoles();
    if (key === "invoices") return loadInvoices();
    if (key === "reports") return loadReports();
    if (key === "inventory") return loadInventory();
    layout("Cài đặt", "Thiết lập chung cho không gian quản trị."); $("#module-body").innerHTML = `<section class="panel module-placeholder"><h2>Cài đặt hệ thống</h2><p>Cấu hình kết nối và tùy chọn chung hiện được quản lý trong cấu hình backend.</p></section>`;
  }
  async function loadAccounts() {
    layout("Quản lý tài khoản", "Tạo tài khoản, thay đổi vai trò và trạng thái truy cập.", `<button class="primary-button" id="create-account" type="button">+ Tạo tài khoản</button>`);
    try { const [users, roles] = await Promise.all([request("/accounts"), request("/roles")]); const roleOptions = roles.map(r => `<option value="${esc(r.name)}">${esc(r.name)}</option>`).join("");
      $("#module-body").innerHTML = `<section class="panel data-panel"><div class="table-scroll"><table><thead><tr><th>TÀI KHOẢN</th><th>EMAIL</th><th>VAI TRÒ</th><th>TRẠNG THÁI</th><th>NGÀY TẠO</th><th></th></tr></thead><tbody>${users.map(u => `<tr data-id="${u.id}"><td class="invoice-id">${esc(u.username)}</td><td>${esc(u.email)}</td><td><select class="inline-select account-role">${roles.map(r => `<option ${u.role === r.name ? "selected" : ""} value="${esc(r.name)}">${esc(r.name)}</option>`).join("")}</select></td><td><button class="status-toggle ${u.isActive ? "enabled" : "disabled"}" data-active="${u.isActive}" type="button">${u.isActive ? "Đang hoạt động" : "Đã khóa"}</button></td><td>${date(u.createdAt)}</td><td><button class="text-action delete-account" type="button">Xóa</button></td></tr>`).join("") || `<tr><td colspan="6">Chưa có tài khoản.</td></tr>`}</tbody></table></div></section>`;
      $("#create-account").onclick = async () => { const form = prompt("Nhập theo thứ tự: tên đăng nhập, email, mật khẩu (tối thiểu 8 ký tự), vai trò (" + roles.map(r => r.name).join(", ") + ")", "newuser, email@example.com, Matkhau123, Customer"); if (!form) return; const [newUsername, email, password, role = "Customer"] = form.split(",").map(x => x.trim()); try { await request("/accounts", { method: "POST", body: JSON.stringify({ username: newUsername, email, password, role }) }); await loadAccounts(); } catch (e) { alert(e.message); } };
      $$(".account-role").forEach(select => select.onchange = async () => { try { await request(`/accounts/${select.closest("tr").dataset.id}`, { method: "PATCH", body: JSON.stringify({ role: select.value }) }); } catch (e) { alert(e.message); } });
      $$(".status-toggle").forEach(button => button.onclick = async () => { try { await request(`/accounts/${button.closest("tr").dataset.id}`, { method: "PATCH", body: JSON.stringify({ isActive: button.dataset.active !== "true" }) }); await loadAccounts(); } catch (e) { alert(e.message); } });
      $$(".delete-account").forEach(button => button.onclick = async () => { if (!confirm("Xóa tài khoản này?")) return; try { await request(`/accounts/${button.closest("tr").dataset.id}`, { method: "DELETE" }); await loadAccounts(); } catch (e) { alert(e.message); } });
    } catch (e) { showError(e); }
  }
  async function loadRoles() {
    layout("Phân quyền người dùng", "Chọn các nhóm chức năng được phép truy cập cho từng vai trò.");
    try { const roles = await request("/roles"); const permissions = [["accounts", "Quản lý tài khoản"], ["permissions", "Phân quyền"], ["invoices", "Quản lý hóa đơn"], ["reports", "Báo cáo, thống kê"], ["inventory", "Tồn kho"]];
      $("#module-body").innerHTML = `<div class="role-grid">${roles.map(role => `<section class="panel role-card" data-role="${esc(role.name)}"><h2>${esc(role.name)}</h2><p>Quyền chức năng</p>${permissions.map(([key, label]) => `<label><input type="checkbox" value="${key}" ${role.permissions.includes(key) ? "checked" : ""}> ${label}</label>`).join("")}<button class="primary-button save-role" type="button">Lưu quyền</button></section>`).join("")}</div>`;
      $$(".save-role").forEach(button => button.onclick = async () => { const card = button.closest(".role-card"); const selected = $$("input:checked", card).map(input => input.value); try { await request(`/roles/${encodeURIComponent(card.dataset.role)}`, { method: "PUT", body: JSON.stringify({ permissions: selected }) }); alert("Đã lưu phân quyền."); } catch (e) { alert(e.message); } });
    } catch (e) { showError(e); }
  }
  async function loadInvoices() {
    layout("Quản lý hóa đơn", "Tạo hóa đơn và cập nhật trạng thái thanh toán.", `<button class="primary-button" id="create-invoice" type="button">+ Tạo hóa đơn</button>`);
    try { const invoices = await request("/invoices");
      $("#module-body").innerHTML = `<section class="panel data-panel"><div class="table-scroll"><table><thead><tr><th>MÃ HÓA ĐƠN</th><th>KHÁCH HÀNG</th><th>NGÀY TẠO</th><th>SỐ TIỀN</th><th>TRẠNG THÁI</th><th>THAO TÁC</th></tr></thead><tbody>${invoices.map(i => `<tr data-id="${i.id}"><td class="invoice-id">${esc(i.code)}</td><td class="invoice-customer">${esc(i.customerName)}</td><td>${date(i.createdAt)}</td><td class="amount invoice-amount" data-amount="${i.amount}">${money(i.amount)}</td><td><select class="inline-select invoice-status" data-id="${i.id}"><option value="Pending" ${i.status === "Pending" ? "selected" : ""}>Chờ thanh toán</option><option value="Paid" ${i.status === "Paid" ? "selected" : ""}>Đã thanh toán</option><option value="Overdue" ${i.status === "Overdue" ? "selected" : ""}>Quá hạn</option><option value="Cancelled" ${i.status === "Cancelled" ? "selected" : ""}>Đã hủy</option></select></td><td><button class="text-action edit-invoice" type="button">Sửa</button> <button class="text-action delete-invoice" type="button">Xóa</button></td></tr>`).join("") || `<tr><td colspan="6">Chưa có hóa đơn.</td></tr>`}</tbody></table></div></section>`;
      $("#create-invoice").onclick = async () => { const customerName = prompt("Tên khách hàng:"); if (!customerName) return; const amount = Number(prompt("Số tiền hóa đơn (VND):")); if (!Number.isFinite(amount) || amount <= 0) return alert("Số tiền không hợp lệ."); try { await request("/invoices", { method: "POST", body: JSON.stringify({ customerName, amount }) }); await loadInvoices(); } catch (e) { alert(e.message); } };
      $$(".invoice-status").forEach(select => select.onchange = async () => { try { await request(`/invoices/${select.dataset.id}`, { method: "PATCH", body: JSON.stringify({ status: select.value }) }); } catch (e) { alert(e.message); } });
      $$(".edit-invoice").forEach(button => button.onclick = async () => { const row = button.closest("tr"); const invoice = invoices.find(item => item.id === row.dataset.id); const customerName = prompt("Tên khách hàng:", invoice.customerName); if (customerName === null) return; const amount = Number(prompt("Số tiền hóa đơn (VND):", String(invoice.amount))); if (!Number.isFinite(amount) || amount <= 0) return alert("Số tiền không hợp lệ."); try { await request(`/invoices/${invoice.id}`, { method: "PATCH", body: JSON.stringify({ customerName, amount }) }); await loadInvoices(); } catch (e) { alert(e.message); } });
      $$(".delete-invoice").forEach(button => button.onclick = async () => { const row = button.closest("tr"); if (!confirm(`Xóa hóa đơn ${row.querySelector(".invoice-id").textContent}?`)) return; try { await request(`/invoices/${row.dataset.id}`, { method: "DELETE" }); await loadInvoices(); } catch (e) { alert(e.message); } });
    } catch (e) { showError(e); }
  }
  async function loadReports() {
    layout("Báo cáo, thống kê", "Số liệu tổng hợp từ các hóa đơn trong hệ thống.");
    try { const report = await request("/reports"); $("#module-body").innerHTML = `<div class="metrics-grid module-metrics"><article class="metric-card"><div class="metric-top">Tổng doanh thu đã thu</div><div class="metric-value">${money(report.totalRevenue)}</div></article><article class="metric-card"><div class="metric-top">Tổng hóa đơn</div><div class="metric-value">${report.invoiceCount.toLocaleString("vi-VN")}</div></article><article class="metric-card"><div class="metric-top">Đã thanh toán</div><div class="metric-value">${report.paidCount.toLocaleString("vi-VN")}</div></article><article class="metric-card"><div class="metric-top">Đang chờ thanh toán</div><div class="metric-value">${money(report.pendingAmount)}</div></article></div><section class="panel data-panel report-table"><h2>Doanh thu theo tháng</h2><div class="table-scroll"><table><thead><tr><th>THÁNG</th><th>SỐ HÓA ĐƠN ĐÃ THU</th><th>DOANH THU</th></tr></thead><tbody>${report.monthly.map(m => `<tr><td>${m.month}/${m.year}</td><td>${m.count}</td><td class="amount">${money(m.revenue)}</td></tr>`).join("") || `<tr><td colspan="3">Chưa có dữ liệu báo cáo.</td></tr>`}</tbody></table></div></section>`; } catch (e) { showError(e); }
  }
  async function loadInventory() {
    layout("Xem tồn kho", "Theo dõi số lượng, giá trị và mức tồn tối thiểu.", `<button class="primary-button" id="create-item" type="button">+ Thêm mặt hàng</button>`);
    try { const items = await request("/inventory"); const low = items.filter(i => i.quantity <= i.reorderLevel).length;
      $("#module-body").innerHTML = `<div class="metrics-grid module-metrics"><article class="metric-card"><div class="metric-top">Mặt hàng</div><div class="metric-value">${items.length}</div></article><article class="metric-card"><div class="metric-top">Sắp hết hàng</div><div class="metric-value">${low}</div></article><article class="metric-card"><div class="metric-top">Tổng số lượng tồn</div><div class="metric-value">${items.reduce((sum, i) => sum + i.quantity, 0).toLocaleString("vi-VN")}</div></article></div><section class="panel data-panel report-table"><div class="table-scroll"><table><thead><tr><th>MÃ SKU</th><th>MẶT HÀNG</th><th>SỐ LƯỢNG</th><th>MỨC TỐI THIỂU</th><th>ĐƠN GIÁ</th><th></th></tr></thead><tbody>${items.map(i => `<tr data-id="${i.id}"><td class="invoice-id">${esc(i.sku)}</td><td>${esc(i.name)}</td><td>${i.quantity} ${i.quantity <= i.reorderLevel ? `<span class="stock-warning">Sắp hết</span>` : ""}</td><td>${i.reorderLevel}</td><td class="amount">${money(i.unitPrice)}</td><td><button class="text-action edit-item" type="button">Sửa</button> <button class="text-action delete-item" type="button">Xóa</button></td></tr>`).join("") || `<tr><td colspan="6">Chưa có mặt hàng.</td></tr>`}</tbody></table></div></section>`;
      const editItem = async (row, existing = null) => { const input = prompt("Nhập theo thứ tự: SKU, tên mặt hàng, số lượng, mức tồn tối thiểu, đơn giá", existing ? `${existing.sku}, ${existing.name}, ${existing.quantity}, ${existing.reorderLevel}, ${existing.unitPrice}` : "SKU-001, Sản phẩm mới, 10, 5, 100000"); if (!input) return; const [sku, name, quantity, reorderLevel, unitPrice] = input.split(",").map(x => x.trim()); const body = { sku, name, quantity: Number(quantity), reorderLevel: Number(reorderLevel), unitPrice: Number(unitPrice) }; try { await request(existing ? `/inventory/${existing.id}` : "/inventory", { method: existing ? "PUT" : "POST", body: JSON.stringify(body) }); await loadInventory(); } catch (e) { alert(e.message); } };
      $("#create-item").onclick = () => editItem(null); $$(".edit-item").forEach(button => button.onclick = () => editItem(button.closest("tr"), items.find(i => i.id === button.closest("tr").dataset.id))); $$(".delete-item").forEach(button => button.onclick = async () => { if (!confirm("Xóa mặt hàng này?")) return; try { await request(`/inventory/${button.closest("tr").dataset.id}`, { method: "DELETE" }); await loadInventory(); } catch (e) { alert(e.message); } });
    } catch (e) { showError(e); }
  }

  $("#admin-logout").addEventListener("click", async () => { try { await fetch(`${api}/api/v1/auth/admin/logout`, { method: "POST", credentials: "include" }); } finally { window.location.replace("../index.html"); } });
  $("#mobile-menu").addEventListener("click", () => $("#sidebar").classList.toggle("open"));
  document.querySelectorAll(".main-nav a, .settings-link").forEach(link => link.addEventListener("click", event => {
    event.preventDefault(); document.querySelectorAll(".main-nav a").forEach(item => { item.classList.remove("active"); item.removeAttribute("aria-current"); });
    if (link.closest(".main-nav")) { link.classList.add("active"); link.setAttribute("aria-current", "page"); }
    openModule(link.getAttribute("href").slice(1)); $("#sidebar").classList.remove("open");
  }));
});
