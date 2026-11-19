import { useEffect, useState, type FormEvent } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import {
  ArrowLeft, ArrowRight, CheckCircle2, Library, Mail,
  RefreshCw, ShieldCheck, Sparkles
} from 'lucide-react';
import { api } from '../services/api';

export default function VerifyEmailPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const state = location.state as { email?: string; sentAt?: number } | null;
  const initialEmail = state?.email || new URLSearchParams(location.search).get('email') || '';
  const [email, setEmail] = useState(initialEmail);
  const [code, setCode] = useState('');
  const [busy, setBusy] = useState(false);
  const [resending, setResending] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [seconds, setSeconds] = useState(
    state?.sentAt ? Math.max(0, 60 - Math.floor((Date.now() - state.sentAt) / 1000)) : 0
  );

  useEffect(() => {
    if (seconds <= 0) return;
    const timer = window.setTimeout(() => setSeconds(v => Math.max(0, v - 1)), 1000);
    return () => window.clearTimeout(timer);
  }, [seconds]);

  async function handleVerify(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError('');
    setMessage('');
    setBusy(true);
    try {
      await api.verifyEmail({ email: email.trim(), code: code.trim() });
      navigate('/dang-nhap', {
        replace: true,
        state: { success: 'Email đã được xác minh. Bạn có thể đăng nhập bằng tài khoản vừa tạo.' }
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Mã xác minh không hợp lệ hoặc đã hết hạn.');
    } finally {
      setBusy(false);
    }
  }

  async function handleResend() {
    setError('');
    setMessage('');
    setResending(true);
    try {
      await api.resendVerification(email.trim());
      setSeconds(60);
      setMessage('Mã xác minh mới đã được yêu cầu gửi đến email của bạn.');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Chưa thể gửi lại mã. Vui lòng thử sau.');
    } finally {
      setResending(false);
    }
  }

  return (
    <main className="auth-page">
      <section className="auth-visual auth-visual-verify">
        <div className="auth-visual-image" />
        <div className="auth-visual-shade" />
        <div className="auth-visual-top">
          <Link to="/" className="auth-brand">
            <span className="auth-brand-mark"><Library size={23} /></span>
            <span>THƯ VIỆN SỐ<small>HỆ THỐNG PHÂN TÁN</small></span>
          </Link>
          <span className="auth-top-pill"><span /> SECURE ACCESS</span>
        </div>
        <div className="auth-visual-content">
          <div className="auth-kicker"><Sparkles size={14} /> MỘT BƯỚC CUỐI CÙNG</div>
          <h1>Xác thực<br /><em>niềm tin.</em></h1>
          <p>
            Mỗi tài khoản là một cánh cửa đến kho tri thức.
            Xác minh email để bảo vệ tài khoản và bắt đầu
            hành trình đọc sách của bạn.
          </p>
          <div className="auth-quote-card"><ShieldCheck size={22} /><span>Bảo vệ tài khoản · Kết nối an toàn · Tri thức mở</span></div>
        </div>
        <div className="auth-photo-caption"><span className="auth-caption-line" />THƯ VIỆN SỐ · KẾT NỐI TRI THỨC</div>
        <div className="auth-visual-footer"><span>VERIFY · PROTECT · DISCOVER</span><span>03 / 03</span></div>
      </section>

      <section className="auth-panel">
        <div className="auth-panel-orb auth-orb-one" />
        <div className="auth-panel-orb auth-orb-two" />
        <div className="auth-panel-inner">
          <Link to="/" className="auth-mobile-brand"><Library size={21} /> THƯ VIỆN SỐ</Link>
          <div className="auth-verify-icon"><Mail size={29} /></div>
          <div className="auth-panel-heading">
            <span className="auth-eyebrow">EMAIL VERIFICATION</span>
            <h2>Xác minh email</h2>
            <p>Nhập mã gồm 6 chữ số được gửi đến hộp thư của bạn.</p>
          </div>

          {message && <div className="auth-alert auth-alert-success"><CheckCircle2 size={17} />{message}</div>}
          {error && <div className="auth-alert auth-alert-error">{error}</div>}

          <form className="auth-form" onSubmit={handleVerify}>
            <label htmlFor="verify-email">Địa chỉ email</label>
            <div className="auth-input-wrap">
              <Mail size={18} />
              <input id="verify-email" type="email" autoComplete="email"
                placeholder="name@gmail.com" value={email}
                onChange={e => setEmail(e.target.value)} required />
            </div>
            <label htmlFor="verify-code">Mã xác minh 6 chữ số</label>
            <input id="verify-code" className="auth-code-input" inputMode="numeric"
              autoComplete="one-time-code" placeholder="000000" maxLength={6}
              value={code} onChange={e => setCode(e.target.value.replace(/\D/g, '').slice(0, 6))}
              required pattern="[0-9]{6}" />
            <div className="auth-code-hint">Mã có hiệu lực trong 5 phút. Kiểm tra cả thư mục Spam/Thư rác nếu chưa thấy email.</div>
            <button className="auth-submit" type="submit" disabled={busy || code.length !== 6}>
              {busy ? 'ĐANG XÁC MINH...' : 'XÁC MINH TÀI KHOẢN'}
              {!busy && <ArrowRight size={18} />}
            </button>
          </form>

          <div className="auth-resend-row">
            <span>Chưa nhận được mã?</span>
            <button type="button" onClick={handleResend} disabled={resending || seconds > 0 || !email.trim()}>
              <RefreshCw size={14} />
              {resending ? 'Đang gửi...' : seconds > 0 ? `Gửi lại sau ${seconds}s` : 'Gửi lại mã'}
            </button>
          </div>
          <Link to="/dang-ky" className="auth-back-link"><ArrowLeft size={16} /> Quay lại đăng ký</Link>
          <div className="auth-panel-bottom"><span>Đã có tài khoản?</span><Link to="/dang-nhap">Đăng nhập</Link></div>
          <div className="auth-copyright">© {new Date().getFullYear()} THƯ VIỆN PHÂN TÁN · KNOWLEDGE FOR EVERYONE</div>
        </div>
      </section>
    </main>
  );
}