const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

document.querySelectorAll("[data-toggle-password]").forEach((button) => {
  button.addEventListener("click", () => {
    const input = document.getElementById(button.dataset.togglePassword);
    if (!input) return;

    const isVisible = input.type === "text";
    input.type = isVisible ? "password" : "text";
    button.textContent = isVisible ? "Hiện" : "Ẩn";
    button.setAttribute("aria-label", isVisible ? "Hiện mật khẩu" : "Ẩn mật khẩu");
  });
});

const loginForm = document.getElementById("login-form");
if (loginForm) {
  loginForm.addEventListener("submit", (event) => {
    event.preventDefault();
    void signInAdmin(loginForm);
  });
}

async function signInAdmin(form) {
  const username = form.elements.username.value.trim();
  const password = form.elements.password.value;
  const message = document.getElementById("login-message");

  if (!username) {
    showMessage(message, "Vui lòng nhập tên đăng nhập.");
    form.elements.username.focus();
    return;
  }
  if (password.length < 8) {
    showMessage(message, "Mật khẩu cần có ít nhất 8 ký tự.");
    form.elements.password.focus();
    return;
  }
  const button = form.querySelector("button[type='submit']");
  button.disabled = true;
  button.setAttribute("aria-busy", "true");
  showMessage(message, "Đang xác thực…");

  try {
    const apiBaseUrl = window.APP_CONFIG?.apiBaseUrl ?? `http://${window.location.hostname}:5000`;
    const response = await fetch(`${apiBaseUrl}/api/v1/auth/admin/login`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username, password })
    });

    if (response.status === 401) {
      showMessage(message, "Tên đăng nhập hoặc mật khẩu không đúng.");
      return;
    }
    if (!response.ok) {
      showMessage(message, "Không thể đăng nhập. Kiểm tra backend rồi thử lại.");
      return;
    }

    window.location.assign("admin/index.html");
  } catch {
    showMessage(message, "Không kết nối được backend tại địa chỉ API đã cấu hình.");
  } finally {
    button.disabled = false;
    button.removeAttribute("aria-busy");
  }
}

const recoveryPanel = document.getElementById("password-recovery");
const recoveryRequestForm = document.getElementById("recovery-request-form");
const recoveryConfirmForm = document.getElementById("recovery-confirm-form");
let recoveryUsername = "";

function syncAuthRoute() {
  const isRecovery = window.location.hash === "#forgot-password";
  document.getElementById("login-view").hidden = isRecovery;
  recoveryPanel.hidden = !isRecovery;
}

window.addEventListener("hashchange", syncAuthRoute);
syncAuthRoute();

document.getElementById("forgot-password-open")?.addEventListener("click", () => {
  recoveryRequestForm.hidden = false;
  recoveryConfirmForm.hidden = true;
  document.getElementById("recovery-code-hint").hidden = true;
  recoveryRequestForm.reset();
  document.getElementById("recovery-request-message").textContent = "";
  document.getElementById("recovery-confirm-message").textContent = "";
});

document.getElementById("back-to-login")?.addEventListener("click", () => {
  window.location.hash = "login";
  syncAuthRoute();
  document.getElementById("login-username").focus();
});

recoveryRequestForm?.addEventListener("submit", async (event) => {
  event.preventDefault();
  const message = document.getElementById("recovery-request-message");
  recoveryUsername = recoveryRequestForm.elements.username.value.trim();
  if (!recoveryUsername) {
    showMessage(message, "Vui lòng nhập tên đăng nhập.");
    recoveryRequestForm.elements.username.focus();
    return;
  }
  const button = recoveryRequestForm.querySelector("button[type='submit']");
  button.disabled = true;
  showMessage(message, "Đang lấy mã OTP…");
  try {
    const apiBaseUrl = window.APP_CONFIG?.apiBaseUrl ?? `http://${window.location.hostname}:5000`;
    const response = await fetch(`${apiBaseUrl}/api/v1/auth/admin/password-reset/request`, {
      method: "POST", headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username: recoveryUsername })
    });
    const result = await response.json().catch(() => ({}));
    if (response.status === 501) {
      showMessage(message, "Khôi phục mật khẩu qua email chưa được cấu hình cho môi trường này.");
      return;
    }
    if (!response.ok) {
      showMessage(message, result.message || "Không thể lấy mã OTP.");
      return;
    }
    if (!result.developmentCode) {
      showMessage(message, "Không tìm thấy tài khoản quản trị phù hợp để cấp mã OTP.");
      return;
    }
    recoveryRequestForm.hidden = true;
    recoveryConfirmForm.hidden = false;
    const hint = document.getElementById("recovery-code-hint");
    hint.innerHTML = `Mã OTP (hết hạn sau 10 phút): <strong>${result.developmentCode}</strong>`;
    hint.hidden = false;
    recoveryConfirmForm.elements.code.focus();
  } catch {
    showMessage(message, "Không kết nối được backend. Hãy kiểm tra API rồi thử lại.");
  } finally {
    button.disabled = false;
  }
});

recoveryConfirmForm?.addEventListener("submit", async (event) => {
  event.preventDefault();
  const message = document.getElementById("recovery-confirm-message");
  const code = recoveryConfirmForm.elements.code.value.trim();
  const newPassword = recoveryConfirmForm.elements.newPassword.value;
  const confirmation = recoveryConfirmForm.elements.confirmPassword.value;
  if (!/^\d{6}$/.test(code)) {
    showMessage(message, "Mã OTP phải gồm 6 chữ số.");
    recoveryConfirmForm.elements.code.focus();
    return;
  }
  if (newPassword.length < 8) {
    showMessage(message, "Mật khẩu mới cần có ít nhất 8 ký tự.");
    recoveryConfirmForm.elements.newPassword.focus();
    return;
  }
  if (newPassword !== confirmation) {
    showMessage(message, "Mật khẩu xác nhận chưa khớp.");
    recoveryConfirmForm.elements.confirmPassword.focus();
    return;
  }
  const button = recoveryConfirmForm.querySelector("button[type='submit']");
  button.disabled = true;
  try {
    const apiBaseUrl = window.APP_CONFIG?.apiBaseUrl ?? `http://${window.location.hostname}:5000`;
    const response = await fetch(`${apiBaseUrl}/api/v1/auth/admin/password-reset/confirm`, {
      method: "POST", headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username: recoveryUsername, code, newPassword })
    });
    const result = await response.json().catch(() => ({}));
    if (!response.ok) {
      showMessage(message, result.message || "Không thể đặt lại mật khẩu.");
      return;
    }
    window.location.hash = "login";
    syncAuthRoute();
    document.getElementById("login-username").value = recoveryUsername;
    document.getElementById("login-password").value = "";
    showMessage(document.getElementById("login-message"), result.message, true);
    recoveryConfirmForm.reset();
  } catch {
    showMessage(message, "Không kết nối được backend. Hãy kiểm tra API rồi thử lại.");
  } finally {
    button.disabled = false;
  }
});

const registerForm = document.getElementById("register-form");
if (registerForm) {
  registerForm.addEventListener("submit", (event) => {
    event.preventDefault();
    const name = registerForm.elements.fullName.value.trim();
    const email = registerForm.elements.email.value.trim();
    const password = registerForm.elements.password.value;
    const confirmation = registerForm.elements.confirmPassword.value;
    const role = registerForm.elements.role.value;
    const message = document.getElementById("register-message");

    if (name.length < 2) {
      showMessage(message, "Vui lòng nhập họ và tên (ít nhất 2 ký tự).");
      registerForm.elements.fullName.focus();
      return;
    }
    if (!emailPattern.test(email)) {
      showMessage(message, "Vui lòng nhập địa chỉ email hợp lệ.");
      registerForm.elements.email.focus();
      return;
    }
    if (password.length < 8) {
      showMessage(message, "Mật khẩu cần có ít nhất 8 ký tự.");
      registerForm.elements.password.focus();
      return;
    }
    if (password !== confirmation) {
      showMessage(message, "Mật khẩu xác nhận chưa khớp.");
      registerForm.elements.confirmPassword.focus();
      return;
    }
    if (!registerForm.elements.terms.checked) {
      showMessage(message, "Vui lòng đồng ý với điều khoản sử dụng để tiếp tục.");
      registerForm.elements.terms.focus();
      return;
    }

    if (role !== "customer") {
      showMessage(message, "Tài khoản nhân viên và quản trị viên được cấp bởi quản trị hệ thống.");
      registerForm.elements.role.focus();
      return;
    }

    showMessage(message, "Biểu mẫu hợp lệ. Chức năng tạo tài khoản sẽ hoạt động khi API tài khoản được triển khai.", true);
  });
}

function showMessage(element, text, success = false) {
  if (!element) return;
  element.textContent = text;
  element.classList.toggle("success", success);
}
