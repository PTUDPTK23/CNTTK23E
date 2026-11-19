import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  ArrowRight, BookOpen, Eye, EyeOff, Library, LockKeyhole,
  Mail, Phone, ShieldCheck, Sparkles, UserRound
} from 'lucide-react';
import { api } from '../services/api';

export default function RegisterPage() {
  const navigate = useNavigate();
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError('');
    if (password.length < 8) {
      setError('Mật khẩu cần có ít nhất 8 ký tự.');
      return;
    }
    if (password !== confirmPassword) {
      setError('Mật khẩu xác nhận chưa khớp.');
      return;
    }
    setBusy(true);
    try {
      await api.register({
        fullName: fullName.trim(),
        email: email.trim(),
        phone: phone.trim() || undefined,
        password
      });
      navigate('/xac-minh-email', {
        state: { email: email.trim(), sentAt: Date.now() },
        replace: true
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không thể tạo tài khoản. Vui lòng thử lại.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="auth-page">
      <section className="auth-visual auth-visual-register">
        <div className="auth-visual-image" />
        <div className="auth-visual-shade" />
        <div className="auth-visual-top">
          <Link to="/" className="auth-brand">
            <span className="auth-brand-mark"><Library size={23} /></span>
            <span>THƯ VIỆN SỐ<small>HỆ THỐNG PHÂN TÁN</small></span>
          </Link>
          <span className="auth-top-pill"><span /> JOIN THE KNOWLEDGE</span>
        </div>
        <div className="auth-visual-content">
          <div className="auth-kicker"><Sparkles size={14} /> MỞ RA THẾ GIỚI TRI THỨC</div>
          <h1>Mỗi cuốn sách<br />là một <em>chân trời.</em></h1>
          <p>
            Tạo tài khoản để khám phá bộ sưu tập sách,
            gửi yêu cầu mượn và theo dõi hành trình đọc
            của riêng bạn.
          </p>
          <div className="auth-quote-card">
            <BookOpen size={22} />
            <span>Học tập, khám phá và phát triển không ngừng.</span>
          </div>
        </div>
        <div className="auth-photo-caption"><span className="auth-caption-line" />DI SẢN TRI THỨC · KHÔNG GIAN HỌC TẬP SỐ</div>
        <div className="auth-visual-footer"><span>YOUR KNOWLEDGE JOURNEY STARTS HERE</span><span>02 / 03</span></div>
      </section>

      <section className="auth-panel">
        <div className="auth-panel-orb auth-orb-one" />
        <div className="auth-panel-orb auth-orb-two" />
        <div className="auth-panel-inner auth-register-inner">
          <Link to="/" className="auth-mobile-brand"><Library size={21} /> THƯ VIỆN SỐ</Link>
          <div className="auth-panel-heading">
            <span className="auth-eyebrow">GIA NHẬP CỘNG ĐỒNG ĐỌC</span>
            <h2>Tạo tài khoản</h2>
            <p>Chỉ mất một phút để bắt đầu.</p>
          </div>
          {error && <div className="auth-alert auth-alert-error">{error}</div>}

          <form className="auth-form" onSubmit={handleSubmit}>
            <label htmlFor="reg-name">Họ và tên</label>
            <div className="auth-input-wrap">
              <UserRound size={18} />
              <input id="reg-name" autoComplete="name" placeholder="Nguyễn Văn An"
                value={fullName} onChange={e => setFullName(e.target.value)} required maxLength={150} />
            </div>

            <label htmlFor="reg-email">Email nhận mã xác minh</label>
            <div className="auth-input-wrap">
              <Mail size={18} />
              <input id="reg-email" type="email" autoComplete="email" placeholder="name@gmail.com"
                value={email} onChange={e => setEmail(e.target.value)} required maxLength={150} />
            </div>

            <label htmlFor="reg-phone">Số điện thoại <span className="auth-optional">Không bắt buộc</span></label>
            <div className="auth-input-wrap">
              <Phone size={18} />
              <input id="reg-phone" type="tel" autoComplete="tel" placeholder="09xx xxx xxx"
                value={phone} onChange={e => setPhone(e.target.value)} />
            </div>

            <label htmlFor="reg-password">Mật khẩu</label>
            <div className="auth-input-wrap">
              <LockKeyhole size={18} />
              <input id="reg-password" type={showPassword ? 'text' : 'password'}
                autoComplete="new-password" placeholder="Ít nhất 8 ký tự"
                value={password} onChange={e => setPassword(e.target.value)} required minLength={8} />
              <button type="button" className="auth-eye" aria-label="Hiện hoặc ẩn mật khẩu"
                onClick={() => setShowPassword(v => !v)}>
                {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
              </button>
            </div>

            <label htmlFor="reg-confirm">Xác nhận mật khẩu</label>
            <div className="auth-input-wrap">
              <ShieldCheck size={18} />
              <input id="reg-confirm" type="password" autoComplete="new-password"
                placeholder="Nhập lại mật khẩu" value={confirmPassword}
                onChange={e => setConfirmPassword(e.target.value)} required minLength={8} />
            </div>

            <button className="auth-submit" type="submit" disabled={busy}>
              {busy ? 'ĐANG TẠO TÀI KHOẢN...' : 'ĐĂNG KÝ & NHẬN MÃ EMAIL'}
              {!busy && <ArrowRight size={18} />}
            </button>
          </form>
          <p className="auth-terms">Khi đăng ký, tài khoản mới được cấp quyền thành viên. Hệ thống sẽ gửi mã xác minh đến email bạn cung cấp.</p>
          <div className="auth-panel-bottom">
            <span>Đã có tài khoản?</span><Link to="/dang-nhap">Đăng nhập ngay</Link>
          </div>
          <div className="auth-copyright">© {new Date().getFullYear()} THƯ VIỆN PHÂN TÁN · KNOWLEDGE FOR EVERYONE</div>
        </div>
      </section>
    </main>
  );
}