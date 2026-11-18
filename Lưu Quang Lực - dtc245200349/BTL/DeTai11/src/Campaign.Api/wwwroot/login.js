"use strict";

const form = document.getElementById("login-form");
const submit = document.getElementById("login-submit");
const error = document.getElementById("login-error");

form.addEventListener("submit", async event => {
  event.preventDefault();
  error.hidden = true;
  submit.disabled = true;
  const fields = new FormData(form);
  try {
    const response = await fetch("/auth/login", {
      method: "POST",
      headers: { "Content-Type": "application/json", "X-NotifyLab-Request": "1" },
      body: JSON.stringify({ username: fields.get("username"), password: fields.get("password") }),
      signal: AbortSignal.timeout(15000)
    });
    if (response.ok) {
      location.replace("/");
      return;
    }
    if (response.status === 429) {
      throw new Error("Bạn đã thử quá nhiều lần. Hãy chờ một phút rồi thử lại.");
    }
    if (response.status === 401) {
      throw new Error("Tên đăng nhập hoặc mật khẩu không đúng.");
    }
    throw new Error("Chưa đăng nhập được. Hãy thử lại.");
  } catch (problem) {
    error.textContent = problem.message;
    error.hidden = false;
    document.getElementById("password").focus();
  } finally {
    submit.disabled = false;
  }
});
