import {
  BookOpen, Home, Library, LogIn, LogOut, Menu, Moon, Radar,
  Search, Settings, Sun, X,
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link, NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useCurrentUser } from '../hooks/UserContext';
import { useTheme } from '../hooks/useTheme';
import { initials, ROLE_LABEL } from '../utils/format';

export function Layout() {
  const { current, logout, loading } = useCurrentUser();
  const { theme, toggle } = useTheme();
  const [open, setOpen] = useState(false);
  const loc = useLocation();
  const navigate = useNavigate();

  useEffect(() => {
    setOpen(false);
    window.scrollTo({ top: 0 });
  }, [loc.pathname]);

  const staff = current?.role === 'ADMIN' || current?.role === 'LIBRARIAN';
  const cls = ({ isActive }: { isActive: boolean }) => (isActive ? 'active' : '');

  function handleLogout() {
    logout();
    navigate('/');
  }

  return (
    <>
      <header className="header">
        <div className="container header-inner">
          <NavLink to="/" className="brand">
            <span className="brand-logo"><Library size={20} /></span>
            <span>Thư viện<small>Phân tán</small></span>
          </NavLink>

          <button className="icon-btn menu-btn" aria-label="Menu" onClick={() => setOpen((o) => !o)}>
            {open ? <X size={22} /> : <Menu size={22} />}
          </button>

          <nav className={open ? 'nav open' : 'nav'}>
            <NavLink to="/" end className={cls}><Home size={16} /> Trang chủ</NavLink>
            <NavLink to="/sach" className={cls}><Search size={16} /> Tra cứu sách</NavLink>
            {current && <NavLink to="/sach-cua-toi" className={cls}><BookOpen size={16} /> Sách của tôi</NavLink>}
            {current && <NavLink to="/yeu-cau" className={cls}><Radar size={16} /> Theo dõi yêu cầu</NavLink>}
            {staff && <NavLink to="/quan-tri" className={cls}><Settings size={16} /> Quản trị</NavLink>}
          </nav>

          <div className="header-tools">
            <button className="icon-btn" aria-label="Đổi giao diện sáng/tối" title="Đổi giao diện sáng/tối" onClick={toggle}>
              {theme === 'dark' ? <Sun size={18} /> : <Moon size={18} />}
            </button>

            {loading ? (
              <span className="muted small">Đang tải...</span>
            ) : current ? (
              <div className="user-select">
                <span className="avatar">{initials(current.fullName)}</span>
                <span className="user-meta">
                  <b>{current.fullName}</b>
                  <small>{ROLE_LABEL[current.role]}</small>
                </span>
                <button className="icon-btn" title="Đăng xuất" aria-label="Đăng xuất" onClick={handleLogout}>
                  <LogOut size={17} />
                </button>
              </div>
            ) : (
              <Link className="btn btn-outline" to="/dang-nhap">
                <LogIn size={16} /> Đăng nhập
              </Link>
            )}
          </div>
        </div>
      </header>

      <main className="container main">
        <Outlet />
      </main>

      <footer className="footer">
        <div className="container footer-inner">
          <div><b>Thư viện phân tán</b> · Đề tài 20 – Hệ thống thư viện và đặt mượn sách phân tán</div>
          <div className="muted">Client → Library API → RabbitMQ → Reservation Worker → PostgreSQL</div>
        </div>
      </footer>
    </>
  );
}
