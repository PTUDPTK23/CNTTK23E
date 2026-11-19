import { useEffect, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import {
  ArrowLeft, ArrowRight, BookOpen, CheckCircle2, Eye, EyeOff,
  Fingerprint, KeyRound, LibraryBig, LockKeyhole, Mail, ShieldCheck,
  Sparkles, UserRound, Wifi,
} from 'lucide-react';
import { api } from '../services/api';
import { useCurrentUser } from '../hooks/UserContext';

function AuthShell({ children, mode }: { children: React.ReactNode; mode: string }) {
  return (
    <main className="auth-screen">
      <section className="auth-visual">
        <div className="auth-visual-image" />
        <div className="auth-visual-shade" />
        <Link to="/" className="auth-brand">
          <span className="auth-brand-icon"><LibraryBig size={25} /></span>
          <span>THƯ VIỆN PHÂN TÁN<small>TRI THỨC · KẾT NỐI · TƯƠNG LAI</small></span>
        </Link>
        <div className="auth-visual-content">
          <span className="auth-kicker"><Sparkles size={14} /> KHÔNG GIAN TRI THỨC SỐ</span>
          <h1>Tri thức hôm nay,<br /><em>kiến tạo ngày mai.</em></h1>
          <p>Nơi lưu giữ giá trị tri thức, kết nối con người với kho tàng sách và công nghệ thư viện hiện đại.</p>
          <div className="auth-visual-quote">
            <span className="auth-quote-mark">“</span>
            <div><strong>Học hỏi là một việc phải tiếp tục suốt đời.</strong><small>Thông điệp về tinh thần học tập và rèn luyện</small></div>
          </div>
        </div>
        <div className="auth-visual-bottom">
          <span><Wifi size={15} /> HỆ THỐNG THƯ VIỆN THÔNG MINH</span>
          <span>EST. 2026</span>
        </div>
      </section>
      <section className="auth-panel">
        <div className="auth-panel-top">
          <Link to="/" className="auth-back"><ArrowLeft size={16} /> Về trang chủ</Link>
          <span className="auth-secure"><ShieldCheck size={15} /> Kết nối bảo mật</span>
        </div>
        <div className="auth-form-wrap" key={mode}>{children}</div>
        <div className="auth-panel-footer">
          <span>© 2026 Thư viện phân tán</span>
          <span>Hệ thống quản lý và đặt mượn sách</span>
        </div>
      </section>
    </main>
  );
}

function AuthHeading({ eyebrow, title, desc }: { eyebrow: string; title: string; desc: string }) {
  return (
    <div className="auth-heading">
      <span className="auth-eyebrow">{eyebrow}</span>
      <h2>{title}</h2>
      <p>{desc}</p>
    </div>
  );
}

function AuthError({ message }: { message: string }) {
  return message ? <div className="auth-alert"><span>!</span>{message}</div> : null;
}

export function LoginPage() {
  const { login, current } = useCurrentUser();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(
    (location.state as { message?: string } | null)?.message || ''
  );

  useEffect(() => {
    if (current) navigate('/', { replace: true });
  }, [current, navigate]);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    setSuccess('');
    setBusy(true);
    try {
      await login({ email: email.trim(), password });
      const next = (location.state as { from?: string } | null)?.from;
      navigate(next || '/', { replace: true });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Đăng nhập không thành công. Vui lòng thử lại.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <AuthShell mode="login">
      <div className="auth-symbol"><Fingerprint size={25} /></div>
      <AuthHeading eyebrow="CHÀO MỪNG BẠN TRỞ LẠI" title="Đăng nhập" desc="Đăng nhập để tiếp tục hành trình khám phá tri thức của bạn." />
      {success && <div className="auth-success"><CheckCircle2 size={17} />{success}</div>}
      <AuthError message={error} />
      <form className="auth-form" onSubmit={submit}>
        <label className="auth-label">Địa chỉ email</label>
        <div className="auth-input-wrap"><Mail size={18} /><input type="email" autoComplete="email" placeholder="name@gmail.com" value={email} onChange={e => setEmail(e.target.value)} required /></div>
        <div className="auth-label-row"><label className="auth-label">Mật khẩu</label></div>
        <div className="auth-input-wrap"><LockKeyhole size={18} /><input type={showPassword ? 'text' : 'password'} autoComplete="current-password" placeholder="Nhập mật khẩu của bạn" value={password} onChange={e => setPassword(e.target.value)} required minLength={8} /><button type="button" className="auth-eye" onClick={() => setShowPassword(v => !v)} aria-label="Hiện hoặc ẩn mật khẩu">{showPassword ? <EyeOff size={18} /> : <Eye size={18} />}</button></div>
        <button className="auth-submit" disabled={busy}>{busy ? 'ĐANG XÁC THỰC...' : 'Đăng nhập hệ thống'} <ArrowRight size={18} /></button>
      </form>
      <div className="auth-switch">Bạn chưa có tài khoản? <Link to="/dang-ky">Đăng ký ngay <ArrowRight size={15} /></Link></div>
      <div className="auth-note"><ShieldCheck size={16} /> Tài khoản được bảo vệ bằng xác thực và mã truy cập bảo mật.</div>
    </AuthShell>
  );
}

export function RegisterPage() {
  const navigate = useNavigate();
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    if (password.length < 8) {
      setError('Mật khẩu cần có ít nhất 8 ký tự.');
      return;
    }
    if (password !== confirm) {
      setError('Mật khẩu xác nhận chưa khớp.');
      return;
    }
    setBusy(true);
    try {
      await api.register({ fullName: fullName.trim(), email: email.trim(), phone: phone.trim() || undefined, password });
      navigate('/dang-nhap', { replace: true, state: { message: 'Đăng ký thành công. Bạn có thể đăng nhập bằng tài khoản vừa tạo.' } });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không thể tạo tài khoản. Vui lòng thử lại.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <AuthShell mode="register">
      <div className="auth-symbol"><UserRound size={25} /></div>
      <AuthHeading eyebrow="BẮT ĐẦU HÀNH TRÌNH TRI THỨC" title="Tạo tài khoản" desc="Đăng ký thành viên để sử dụng các dịch vụ thư viện trực tuyến." />
      <AuthError message={error} />
      <form className="auth-form" onSubmit={submit}>
        <label className="auth-label">Họ và tên</label>
        <div className="auth-input-wrap"><UserRound size={18} /><input autoComplete="name" placeholder="Nguyễn Văn A" value={fullName} onChange={e => setFullName(e.target.value)} required maxLength={150} /></div>
        <label className="auth-label">Địa chỉ email nhận mã xác minh</label>
        <div className="auth-input-wrap"><Mail size={18} /><input type="email" autoComplete="email" placeholder="name@gmail.com" value={email} onChange={e => setEmail(e.target.value)} required maxLength={150} /></div>
        <label className="auth-label">Số điện thoại <span>(không bắt buộc)</span></label>
        <div className="auth-input-wrap"><UserRound size={18} /><input type="tel" autoComplete="tel" placeholder="09xxxxxxxx" value={phone} onChange={e => setPhone(e.target.value)} /></div>
        <label className="auth-label">Mật khẩu</label>
        <div className="auth-input-wrap"><LockKeyhole size={18} /><input type={showPassword ? 'text' : 'password'} autoComplete="new-password" placeholder="Tối thiểu 8 ký tự" value={password} onChange={e => setPassword(e.target.value)} required minLength={8} /><button type="button" className="auth-eye" onClick={() => setShowPassword(v => !v)} aria-label="Hiện hoặc ẩn mật khẩu">{showPassword ? <EyeOff size={18} /> : <Eye size={18} />}</button></div>
        <label className="auth-label">Nhập lại mật khẩu</label>
        <div className="auth-input-wrap"><KeyRound size={18} /><input type="password" autoComplete="new-password" placeholder="Nhập lại mật khẩu" value={confirm} onChange={e => setConfirm(e.target.value)} required /></div>
        <div className="auth-role-info"><ShieldCheck size={16} /> Tài khoản thành viên được cấp quyền USER sau khi xác minh email.</div>
        <button className="auth-submit" disabled={busy}>{busy ? 'ĐANG TẠO TÀI KHOẢN...' : 'Tạo tài khoản & gửi mã'} <ArrowRight size={18} /></button>
      </form>
      <div className="auth-switch">Đã có tài khoản? <Link to="/dang-nhap">Đăng nhập <ArrowRight size={15} /></Link></div>
    </AuthShell>
  );
}

export function VerifyEmailPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const state = location.state as { email?: string; justRegistered?: boolean } | null;
  const [email, setEmail] = useState(state?.email || '');
  const [code, setCode] = useState('');
  const [busy, setBusy] = useState(false);
  const [resending, setResending] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState(state?.justRegistered ? 'Mã xác minh đã được yêu cầu gửi đến email của bạn. Hãy kiểm tra hộp thư đến và thư rác.' : '');
  const [countdown, setCountdown] = useState(state?.justRegistered ? 60 : 0);

  useEffect(() => {
    if (countdown <= 0) return;
    const timer = window.setTimeout(() => setCountdown(v => v - 1), 1000);
    return () => window.clearTimeout(timer);
  }, [countdown]);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    setBusy(true);
    try {
      await api.verifyEmail({ email: email.trim(), code: code.trim() });
      navigate('/dang-nhap', { replace: true, state: { message: 'Email đã được xác minh thành công. Bạn có thể đăng nhập bằng tài khoản vừa tạo.' } });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Mã xác minh không hợp lệ hoặc đã hết hạn.');
    } finally {
      setBusy(false);
    }
  }

  async function resend() {
    setError('');
    setNotice('');
    setResending(true);
    try {
      const message = await api.resendVerification(email.trim());
      setNotice(typeof message === 'string' ? message : 'Đã yêu cầu gửi lại mã xác minh. Vui lòng kiểm tra email.');
      setCountdown(60);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không thể gửi lại mã. Vui lòng thử sau.');
    } finally {
      setResending(false);
    }
  }

  return (
    <AuthShell mode="verify">
      <div className="auth-symbol"><Mail size={25} /></div>
      <AuthHeading eyebrow="BẢO VỆ TÀI KHOẢN CỦA BẠN" title="Xác minh email" desc="Nhập mã xác minh được gửi đến địa chỉ email bạn đã đăng ký." />
      {notice && <div className="auth-success"><CheckCircle2 size={17} />{notice}</div>}
      <AuthError message={error} />
      <form className="auth-form" onSubmit={submit}>
        <label className="auth-label">Địa chỉ email</label>
        <div className="auth-input-wrap"><Mail size={18} /><input type="email" placeholder="name@gmail.com" value={email} onChange={e => setEmail(e.target.value)} required /></div>
        <label className="auth-label">Mã xác minh 6 chữ số</label>
        <input className="auth-otp" inputMode="numeric" autoComplete="one-time-code" maxLength={6} placeholder="••••••" value={code} onChange={e => setCode(e.target.value.replace(/\D/g, '').slice(0, 6))} required />
        <p className="auth-hint">Mã có hiệu lực trong 5 phút. Không chia sẻ mã này với người khác.</p>
        <button className="auth-submit" disabled={busy || code.length !== 6}>{busy ? 'ĐANG XÁC MINH...' : 'Xác minh tài khoản'} <ArrowRight size={18} /></button>
      </form>
      <div className="auth-resend">
        <span>Chưa nhận được mã?</span>
        <button type="button" onClick={resend} disabled={resending || countdown > 0 || !email.trim()}>
          {resending ? 'Đang gửi...' : countdown > 0 ? `Gửi lại sau ${countdown}s` : 'Gửi lại mã'}
        </button>
      </div>
      <div className="auth-switch"><Link to="/dang-ky"><ArrowLeft size={15} /> Quay lại đăng ký</Link></div>
    </AuthShell>
  );
}