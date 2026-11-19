import { useState, type FormEvent } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import {
  ArrowRight, BookOpen, Eye, EyeOff, Fingerprint,
  GraduationCap, Library, LockKeyhole, Mail, ShieldCheck, Sparkles
} from 'lucide-react';
import { useCurrentUser } from '../hooks/UserContext';

export default function LoginPage() {
  const { login } = useCurrentUser();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [success] = useState(
    (location.state as { success?: string } | null)?.success ?? ''
  );

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError('');
    setBusy(true);
    try {
      await login({ email: email.trim(), password });
      const next = (location.state as { from?: string } | null)?.from;
      navigate(next || '/', { replace: true });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Đăng nhập chưa thành công. Vui lòng thử lại.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="auth-page">
      <section className="auth-visual">
        <div className="auth-visual-image" />
        <div className="auth-visual-shade" />
        <div className="auth-visual-top">
          <Link to="/" className="auth-brand">
            <span className="auth-brand-mark"><Library size={23} /></span>
            <span>THƯ VIỆN SỐ<small>HỆ THỐNG PHÂN TÁN</small></span>
          </Link>
          <span className="auth-top-pill"><span /> DIGITAL KNOWLEDGE SPACE</span>
        </div>

        <div className="auth-visual-content">
          <div className="auth-kicker"><Sparkles size={14} /> TRI THỨC LÀ SỨC MẠNH</div>
          <h1>Nơi tri thức<br />được <em>kết nối.</em></h1>
          <p>
            Từ giá trị của tri thức và tinh thần hiếu học,
            kiến tạo một không gian đọc, khám phá và chia sẻ
            hiện đại cho thế hệ hôm nay.
          </p>
          <div className="auth-visual-stats">
            <div><BookOpen size={19} /><span><b>Kho tri thức</b><small>Tra cứu mọi lúc</small></span></div>
            <div><Fingerprint size={19} /><span><b>Bảo mật tài khoản</b><small>Xác thực an toàn</small></span></div>
          </div>
        </div>

        <div className="auth-photo-caption">
          <span className="auth-caption-line" />
          <span>Tư liệu lịch sử · Chủ tịch Hồ Chí Minh đọc và nghiên cứu sách</span>
        </div>
        <div className="auth-visual-footer">
          <span>DI SẢN TRI THỨC · TƯƠNG LAI SỐ</span>
          <span>01 / 03</span>
        </div>
      </section>

      <section className="auth-panel">
        <div className="auth-panel-orb auth-orb-one" />
        <div className="auth-panel-orb auth-orb-two" />
        <div className="auth-panel-inner">
          <Link to="/" className="auth-mobile-brand"><Library size={21} /> THƯ VIỆN SỐ</Link>
          <div className="auth-panel-heading">
            <span className="auth-eyebrow">CHÀO MỪNG QUAY TRỞ LẠI</span>
            <h2>Đăng nhập</h2>
            <p>Tiếp tục hành trình khám phá kho tàng tri thức.</p>
          </div>

          {success && <div className="auth-alert auth-alert-success"><ShieldCheck size={17} />{success}</div>}
          {error && <div className="auth-alert auth-alert-error">{error}</div>}

          <form className="auth-form" onSubmit={handleSubmit}>
            <label htmlFor="login-email">Địa chỉ email</label>
            <div className="auth-input-wrap">
              <Mail size={18} />
              <input id="login-email" type="email" autoComplete="email"
                placeholder="name@gmail.com" value={email}
                onChange={e => setEmail(e.target.value)} required />
            </div>

            <div className="auth-label-row">
              <label htmlFor="login-password">Mật khẩu</label>
            </div>
            <div className="auth-input-wrap">
              <LockKeyhole size={18} />
              <input id="login-password" type={showPassword ? 'text' : 'password'}
                autoComplete="current-password" placeholder="Nhập mật khẩu của bạn"
                value={password} onChange={e => setPassword(e.target.value)} required />
              <button type="button" className="auth-eye" aria-label="Hiện hoặc ẩn mật khẩu"
                onClick={() => setShowPassword(v => !v)}>
                {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
              </button>
            </div>

            <button className="auth-submit" type="submit" disabled={busy}>
              {busy ? 'ĐANG XÁC THỰC...' : 'ĐĂNG NHẬP'}
              {!busy && <ArrowRight size={18} />}
            </button>
          </form>

          <div className="auth-separator"><span>CHƯA CÓ TÀI KHOẢN?</span></div>
          <Link to="/dang-ky" className="auth-secondary-button">
            Tạo tài khoản thư viện <ArrowRight size={17} />
          </Link>
          <div className="auth-panel-bottom">
            <div><ShieldCheck size={15} /> Kết nối bảo mật bằng JWT</div>
            <Link to="/">Quay về trang chủ</Link>
          </div>
          <div className="auth-copyright">© {new Date().getFullYear()} THƯ VIỆN PHÂN TÁN · KNOWLEDGE FOR EVERYONE</div>
        </div>
      </section>
    </main>
  );
}