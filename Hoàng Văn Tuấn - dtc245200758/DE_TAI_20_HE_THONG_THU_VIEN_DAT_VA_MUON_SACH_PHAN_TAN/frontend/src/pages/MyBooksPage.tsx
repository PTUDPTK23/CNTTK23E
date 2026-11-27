import { Ban, BookMarked, CalendarClock, CheckCircle2, Hourglass } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { BookCover } from '../components/BookCover';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { EmptyState, ErrorState, Loading } from '../components/Feedback';
import { StatCard } from '../components/StatCard';
import { StatusBadge } from '../components/StatusBadge';
import { useToast } from '../hooks/ToastContext';
import { useAsync } from '../hooks/useAsync';
import { useCurrentUser } from '../hooks/UserContext';
import { api } from '../services/api';
import type { ReservationStatus } from '../types';
import { daysUntil, errMsg, formatDay, STATUS_LABEL, timeAgo } from '../utils/format';

type Tab = 'ALL' | ReservationStatus;
const TABS: Tab[] = ['ALL', 'PENDING', 'RESERVED', 'BORROWED', 'RETURNED', 'REJECTED', 'CANCELLED'];

export default function MyBooksPage() {
  const toast = useToast();
  const { current, loading: userLoading } = useCurrentUser();
  const uid = current?.id;
  const list = useAsync(() => (uid ? api.userReservations(uid) : Promise.resolve([])), [uid]);
  const books = useAsync(() => api.books({ size: 50 }), []);
  const [tab, setTab] = useState<Tab>('ALL');
  const [toCancel, setToCancel] = useState<string | null>(null);

  const hasPending = list.data?.some((r) => r.status === 'PENDING');
  useEffect(() => {
    if (!hasPending) return;
    const t = setInterval(() => list.refresh(), 3000);
    return () => clearInterval(t);
  }, [hasPending]); // eslint-disable-line react-hooks/exhaustive-deps

  async function doCancel() {
    const id = toCancel;
    setToCancel(null);
    if (!id) return;
    try { await api.cancel(id); toast.push('success', 'Đã hủy yêu cầu.'); list.refresh(); }
    catch (e) { toast.push('error', errMsg(e)); }
  }

  if (userLoading || list.loading) return <Loading />;
  if (!current) return <EmptyState text="Chưa có người dùng. Hãy kiểm tra kết nối API." />;
  if (list.error) return <ErrorState message={list.error} onRetry={list.reload} />;
  const all = list.data ?? [];
  const count = (s: ReservationStatus) => all.filter((r) => r.status === s).length;
  const overdue = all.filter((r) => r.status === 'BORROWED' && (daysUntil(r.dueDate) ?? 1) < 0).length;
  const shown = tab === 'ALL' ? all : all.filter((r) => r.status === tab);
  const cover = (bookId: string) => books.data?.items.find((b) => b.id === bookId);

  return (
    <>
      <div className="page-head"><h1>Sách của tôi</h1><p className="muted">Các sách bạn đã đặt, đang mượn và lịch sử — {current.fullName}</p></div>
      <section className="grid stats">
        <StatCard icon={<Hourglass size={22} />} label="Đang chờ xử lý" value={count('PENDING')} tone="warn" />
        <StatCard icon={<CheckCircle2 size={22} />} label="Đã giữ sách" value={count('RESERVED')} tone="info" />
        <StatCard icon={<BookMarked size={22} />} label="Đang mượn" value={count('BORROWED')} tone="violet" hint={overdue ? `${overdue} quá hạn` : undefined} />
        <StatCard icon={<CalendarClock size={22} />} label="Đã trả" value={count('RETURNED')} tone="ok" />
      </section>

      <div className="tabs">
        {TABS.map((s) => (
          <button key={s} className={tab === s ? 'tab active' : 'tab'} onClick={() => setTab(s)}>
            {s === 'ALL' ? 'Tất cả' : STATUS_LABEL[s]} ({s === 'ALL' ? all.length : count(s)})
          </button>
        ))}
      </div>

      {shown.length === 0 ? <EmptyState text="Không có mục nào trong nhóm này."><Link className="btn btn-outline" to="/sach">Tra cứu sách</Link></EmptyState> : (
        <div className="list">
          {shown.map((r) => {
            const b = cover(r.bookId);
            const left = r.status === 'BORROWED' ? daysUntil(r.dueDate) : null;
            return (
              <div key={r.id} className="book-row static">
                <BookCover title={r.bookTitle} src={b?.coverImage} className="row-cover" />
                <div className="grow">
                  <Link to={`/sach/${r.bookId}`}><h3>{r.bookTitle}</h3></Link>
                  <p className="muted small">{r.bookAuthor} · gửi {timeAgo(r.requestedAt)}</p>
                  <div className="row tight">
                    <StatusBadge status={r.status} />
                    {r.copyCode && <span className="chip">Bản {r.copyCode}</span>}
                    {r.dueDate && <span className={left !== null && left < 0 ? 'chip chip-bad' : 'chip'}>Hạn trả {formatDay(r.dueDate)}{left !== null ? (left < 0 ? ` · quá ${-left} ngày` : ` · còn ${left} ngày`) : ''}</span>}
                  </div>
                  {r.rejectionReason && <p className="small err">{r.rejectionReason}</p>}
                </div>
                <div className="actions">
                  <Link className="btn btn-outline" to={`/yeu-cau/${r.id}`}>Chi tiết</Link>
                  {(r.status === 'PENDING' || r.status === 'RESERVED') && <button className="btn btn-danger" onClick={() => setToCancel(r.id)}><Ban size={14} /> Hủy</button>}
                </div>
              </div>
            );
          })}
        </div>
      )}
      <ConfirmDialog open={toCancel !== null} danger title="Hủy yêu cầu" message="Bạn có chắc muốn hủy yêu cầu này?"
        confirmText="Hủy yêu cầu" onConfirm={doCancel} onCancel={() => setToCancel(null)} />
    </>
  );
}
