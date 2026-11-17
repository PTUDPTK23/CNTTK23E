const form = document.querySelector('#auth-form');
const emailInput = document.querySelector('#email');
const passwordInput = document.querySelector('#password');
const rememberInput = document.querySelector('#remember');
const message = document.querySelector('#form-message');
const submitButton = form.querySelector('button[type="submit"]');
const toggleButton = document.querySelector('.toggle-password');
const page = new URLSearchParams(location.search).get('page') || 'login';
const isRegister = page === 'register';
const isForgot = page === 'forgot';
const API_BASE = location.port === '5000' ? '' : 'http://localhost:5000';

if (isRegister || isForgot) {
  document.querySelector('title').textContent = `${isRegister ? 'Tạo tài khoản' : 'Khôi phục mật khẩu'} · Hệ thống giám sát IoT`;
  document.querySelector('.form-kicker').textContent = isRegister ? 'BẮT ĐẦU SỬ DỤNG' : 'KHÔI PHỤC TÀI KHOẢN';
  document.querySelector('h1').textContent = isRegister ? 'Tạo tài khoản của bạn' : 'Quên mật khẩu?';
  document.querySelector('.form-subtitle').textContent = isRegister ? 'Tham gia hệ thống để theo dõi mọi cảm biến.' : 'Nhập email để nhận hướng dẫn khôi phục tài khoản.';
  document.querySelector('.password-heading').hidden = isForgot;
  document.querySelector('.password-heading span').textContent = isRegister ? 'Mật khẩu' : '';
  document.querySelector('.password-wrap').hidden = !isRegister;
  document.querySelector('.form-options').hidden = true;
  document.querySelector('.or-divider').hidden = true;
  toggleButton.hidden = !isRegister;
  emailInput.type = 'email';
  emailInput.placeholder = 'ban@congty.com';
  emailInput.autocomplete = 'email';
  submitButton.querySelector('span').textContent = isRegister ? 'Tạo tài khoản' : 'Gửi hướng dẫn khôi phục';
  document.querySelector('.register-prompt').innerHTML = isRegister
    ? 'Bạn đã có tài khoản? <a href="/">Đăng nhập <span aria-hidden="true">→</span></a>'
    : 'Bạn đã nhớ mật khẩu? <a href="/">Quay lại đăng nhập <span aria-hidden="true">→</span></a>';
}

const savedEmail = localStorage.getItem('iot-remembered-email');
if (savedEmail) { emailInput.value = savedEmail; rememberInput.checked = true; }

toggleButton.addEventListener('click', () => {
  const show = passwordInput.type === 'password';
  passwordInput.type = show ? 'text' : 'password';
  toggleButton.setAttribute('aria-label', show ? 'Ẩn mật khẩu' : 'Hiện mật khẩu');
  toggleButton.setAttribute('aria-pressed', String(show));
});

const validEmail = value => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
function showMessage(text, error = false) {
  message.textContent = text;
  message.classList.toggle('error', error);
}

form.addEventListener('submit', async event => {
  event.preventDefault();
  const email = emailInput.value.trim();
  const password = passwordInput.value;
  showMessage('');
  if (!email || ((page === 'login' || isRegister) && !password)) {
    showMessage(page === 'login' ? 'Vui lòng nhập tên đăng nhập và mật khẩu.' : 'Vui lòng nhập email và mật khẩu.', true);
    (!email ? emailInput : passwordInput).focus();
    return;
  }
  if ((isRegister || isForgot || (page === 'login' && email.includes('@'))) && !validEmail(email)) {
    showMessage('Vui lòng nhập địa chỉ email hợp lệ.', true); emailInput.focus(); return;
  }
  const endpoint = isRegister ? '/api/auth/register' : isForgot ? '/api/auth/forgot-password' : '/api/auth/login';
  const payload = isForgot ? { email } : { email, password };
  submitButton.disabled = true;
  const buttonText = submitButton.querySelector('span');
  const originalText = buttonText.textContent;
  buttonText.textContent = isForgot ? 'Đang gửi…' : isRegister ? 'Đang tạo tài khoản…' : 'Đang đăng nhập…';
  try {
    const response = await fetch(`${API_BASE}${endpoint}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) });
    const result = await response.json();
    if (!response.ok) throw new Error(result.message || 'Đã xảy ra lỗi. Vui lòng thử lại.');
    if (page === 'login') {
      if (rememberInput.checked) localStorage.setItem('iot-remembered-email', email);
      else localStorage.removeItem('iot-remembered-email');
      sessionStorage.setItem('iot-session-token', result.token);
      sessionStorage.setItem('iot-session-user', JSON.stringify(result.user));
    }
    showMessage(result.message);
  } catch (error) {
    showMessage(error.message === 'Failed to fetch' ? 'Không thể kết nối máy chủ. Vui lòng thử lại.' : error.message, true);
  } finally { submitButton.disabled = false; buttonText.textContent = originalText; }
});
