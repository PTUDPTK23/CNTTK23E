import { Activity, BookCopy, Layers, ShieldAlert, Tags, Users } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { EmptyState, Notice } from '../components/Feedback';
import { useCurrentUser } from '../hooks/UserContext';
import BooksTab from './admin/BooksTab';
import CategoriesTab from './admin/CategoriesTab';
import OverviewTab from './admin/OverviewTab';
import ReservationsTab from './admin/ReservationsTab';
import UsersTab from './admin/UsersTab';

type Tab = 'overview' | 'books' | 'reservations' | 'users' | 'categories';
const TABS: { id: Tab; label: string; icon: typeof Activity }[] = [
  { id: 'overview', label: 'Tổng quan', icon: Activity },
  { id: 'books', label: 'Sách & bản sao', icon: BookCopy },
  { id: 'reservations', label: 'Yêu cầu đặt mượn', icon: Layers },
  { id: 'users', label: 'Người dùng', icon: Users },
  { id: 'categories', label: 'Thể loại', icon: Tags },
];

/** Trang quản trị. Hiện chỉ ẩn ở giao diện theo vai trò (chưa có xác thực/JWT ở backend - xem docs/architecture.md). */
export default function AdminPage() {
  const { current } = useCurrentUser();
  const [tab, setTab] = useState<Tab>('overview');
  if (current && current.role === 'USER') {
    return (
      <EmptyState text="Trang này dành cho Thủ thư / Quản trị viên.">
        <Notice kind="info">Hãy chọn người dùng có vai trò <b>Thủ thư</b> hoặc <b>Quản trị viên</b> ở góc phải phía trên. <Link to="/">Về trang chủ</Link></Notice>
      </EmptyState>
    );
  }
  return (
    <>
      <div className="page-head"><h1>Trung tâm quản trị</h1>
        <p className="muted"><ShieldAlert size={14} /> Đang thao tác với vai trò: {current?.fullName ?? '—'}</p></div>
      <div className="tabs">
        {TABS.map(({ id, label, icon: I }) => <button key={id} className={tab === id ? 'tab active' : 'tab'} onClick={() => setTab(id)}><I size={15} /> {label}</button>)}
      </div>
      {tab === 'overview' && <OverviewTab />}
      {tab === 'books' && <BooksTab />}
      {tab === 'reservations' && <ReservationsTab />}
      {tab === 'users' && <UsersTab />}
      {tab === 'categories' && <CategoriesTab />}
    </>
  );
}
