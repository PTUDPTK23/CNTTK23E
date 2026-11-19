import { Ban, CalendarClock, RefreshCw, Search } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { EmptyState, ErrorState, Loading, Notice } from '../components/Feedback';
import { LifecycleStepper } from '../components/LifecycleStepper';
import { StatusBadge } from '../components/StatusBadge';
import { useToast } from '../hooks/ToastContext';
import { useAsync } from '../hooks/useAsync';
import { useCurrentUser } from '../hooks/UserContext';
import { api } from '../services/api';
import { daysUntil, errMsg, formatDate, STATUS_COLOR, timeAgo } from '../utils/format';

function Detail({ id, justCreated }: { id: string; justCreated: boolean }) {
  const toast = useToast();
  const res = useAsync(() => api.reservation(id), [id]);
  const events = useAsync(() => api.reservationEvents(id), [id]);
  const [cancel, setCancel] = useState(false);
  const status = res.data?.status;

  // Tự động cập nhật (polling 2 giây) cho tới khi Worker xử lý xong (hết PENDING).
  useEffect(() => {
    if (status !== 'PENDING') return;
    const t = setInterval(() => { res.refresh(); events.refresh(); }, 2000);
    return () => clearInterval(t);
  }, [status]); // eslint-disable-line react-hooks/exhaustive-deps

  async function doCancel() {
    setCancel(false);
    try { await api.cancel(id); toast.push('success', 'Đã hủy yêu cầu.'); res.refresh(); events.refresh(); }
    catch (e) { toast.push('error', errMsg(e)); }
  }

  if (res.loading) return <Loading />;
  if (res.error || !res.data) return <ErrorState message={res.error ?? 'Không tìm thấy yêu cầu'} onRetry={res.reload} />;
  const r = res.data;
  const left = r.status === 'BORROWED' ? daysUntil(r.dueDate) : null;
  return (
    <>
      {justCreated && r.status === 'PENDING' && <Notice kind="success">Yêu cầu đã được tiếp nhận (HTTP 202). Hệ thống đang xử lý bất đồng bộ — trang này tự cập nhật.</Notice>}
      <div className="card">
        <div className="row between">
          <div>
            <h2 className="serif">{r.bookTitle}</h2>
            <p className="muted">{r.bookAuthor}</p>
          </div>
          <StatusBadge status={r.status} />
        </div>
        <LifecycleStepper r={r} />
        {r.status === 'PENDING' && <Notice kind="info">Đang chờ Reservation Worker xử lý (thường chỉ vài giây)...</Notice>}
        {r.status === 'REJECTED' && <Notice kind="error">Lý do từ chối: {r.rejectionReason ?? 'Không rõ'}</Notice>}
        {r.status === 'RESERVED' && <Notice kind="success">Đã giữ bản sao <b>{r.copyCode}</b> cho bạn. Hãy đến thư viện nhận sách trong thời hạn giữ chỗ.</Notice>}
        {r.status === 'BORROWED' && left !== null && (
          <Notice kind={left < 0 ? 'error' : left <= 3 ? 'warn' : 'info'}>
            <CalendarClock size={14} /> {left < 0 ? `Đã quá hạn ${-left} ngày — vui lòng trả sách sớm.` : `Còn ${left} ngày đến hạn trả (${formatDate(r.dueDate)}).`}
          </Notice>
        )}
        {r.status === 'CANCELLED' && <Notice kind="warn">Yêu cầu đã bị hủy.</Notice>}
        <dl className="meta meta-3">
          <div><dt>Mã yêu cầu</dt><dd><code>{r.id}</code></dd></div>
          <div><dt>Người mượn</dt><dd>{r.userName}</dd></div>
          <div><dt>Bản sao</dt><dd>{r.copyCode ?? '—'}</dd></div>
          <div><dt>Ngày gửi</dt><dd>{formatDate(r.requestedAt)}</dd></div>
          <div><dt>Worker xử lý lúc</dt><dd>{formatDate(r.processedAt)}</dd></div>
          <div><dt>Hạn trả</dt><dd>{formatDate(r.dueDate)}</dd></div>
          <div><dt>Ngày nhận</dt><dd>{formatDate(r.borrowedAt)}</dd></div>
          <div><dt>Ngày trả</dt><dd>{formatDate(r.returnedAt)}</dd></div>
        </dl>
        <div className="row">
          <button className="btn btn-outline" onClick={() => { res.refresh(); events.refresh(); }}><RefreshCw size={15} /> Làm mới</button>
          {(r.status === 'PENDING' || r.status === 'RESERVED') && <button className="btn btn-danger" onClick={() => setCancel(true)}><Ban size={15} /> Hủy yêu cầu</button>}
          <Link className="btn btn-outline" to={`/sach/${r.bookId}`}>Xem sách</Link>
        </div>
      </div>

      <h3 className="section-title">Lịch sử trạng thái</h3>
      <div className="card">
        <ol className="timeline">
          {events.data?.map((e) => (
            <li key={e.id}>
              <span className="tl-dot" style={{ background: STATUS_COLOR[e.newStatus as keyof typeof STATUS_COLOR] ?? 'var(--muted)' }} />
              <div>
                <b>{e.newStatus}</b> <span className="muted small">· {timeAgo(e.createdAt)} · {formatDate(e.createdAt)} · {e.actorType}</span>
                {e.note && <div className="small">{e.note}</div>}
              </div>
            </li>
          ))}
        </ol>
      </div>
      <ConfirmDialog open={cancel} danger title="Hủy yêu cầu" message="Bạn có chắc muốn hủy yêu cầu này?" confirmText="Hủy yêu cầu" onConfirm={doCancel} onCancel={() => setCancel(false)} />
    </>
  );
}

export default function TrackPage() {
  const { id } = useParams();
  const nav = useNavigate();
  const loc = useLocation();
  const { current } = useCurrentUser();
  const [input, setInput] = useState('');
  const recent = useAsync(() => (current ? api.userReservations(current.id) : Promise.resolve([])), [current?.id]);
  return (
    <>
      <div className="page-head"><h1>Theo dõi yêu cầu đặt mượn</h1><p className="muted">Nhập mã yêu cầu hoặc chọn từ danh sách gần đây của bạn.</p></div>
      <form className="card row search-row" onSubmit={(e) => { e.preventDefault(); if (input.trim()) nav(`/yeu-cau/${input.trim()}`); }}>
        <Search size={18} className="muted" />
        <input className="grow" placeholder="Nhập mã yêu cầu (UUID)" value={input} onChange={(e) => setInput(e.target.value)} />
        <button className="btn" type="submit">Tra cứu</button>
      </form>
      {!id && (
        recent.data && recent.data.length > 0 ? (
          <>
            <h3 className="section-title">Yêu cầu gần đây của {current?.fullName}</h3>
            <div className="list">
              {recent.data.slice(0, 6).map((r) => (
                <Link key={r.id} to={`/yeu-cau/${r.id}`} className="book-row">
                  <div className="grow"><h3>{r.bookTitle}</h3><p className="muted small">{timeAgo(r.requestedAt)} · <code>{r.id.slice(0, 8)}</code></p></div>
                  <StatusBadge status={r.status} />
                </Link>
              ))}
            </div>
          </>
        ) : !recent.loading && <EmptyState text="Chưa có yêu cầu nào. Hãy đặt mượn một cuốn sách để bắt đầu." />
      )}
      {id && <Detail key={id} id={id} justCreated={Boolean((loc.state as { justCreated?: boolean } | null)?.justCreated)} />}
    </>
  );
}
