import { BookMarked, Ban, RefreshCw, Search, Undo2 } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { EmptyState, ErrorState, Loading } from '../../components/Feedback';
import { StatusBadge } from '../../components/StatusBadge';
import { useToast } from '../../hooks/ToastContext';
import { useAsync } from '../../hooks/useAsync';
import { api } from '../../services/api';
import type { ReservationStatus } from '../../types';
import { daysUntil, errMsg, formatDay, STATUS_LABEL, timeAgo } from '../../utils/format';

export default function ReservationsTab() {
  const toast = useToast();
  const [status, setStatus] = useState<ReservationStatus | ''>('');
  const [kw, setKw] = useState('');
  const list = useAsync(() => api.allReservations(status || undefined), [status]);
  const stats = useAsync(() => api.stats(), []);

  useEffect(() => {
    const t = setInterval(() => { list.refresh(); stats.refresh(); }, 5000);
    return () => clearInterval(t);
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  async function act(fn: () => Promise<unknown>, ok: string) {
    try { await fn(); toast.push('success', ok); list.refresh(); stats.refresh(); }
    catch (e) { toast.push('error', errMsg(e)); }
  }

  const k = kw.trim().toLowerCase();
  const rows = (list.data ?? []).filter((r) => !k || `${r.userName} ${r.bookTitle} ${r.id} ${r.copyCode ?? ''}`.toLowerCase().includes(k));
  const all = stats.data?.reservationsByStatus;
  const total = all ? Object.values(all).reduce((a, b) => a + b, 0) : undefined;

  return (
    <>
      <div className="tabs">
        <button className={status === '' ? 'tab active' : 'tab'} onClick={() => setStatus('')}>Tất cả{total !== undefined ? ` (${total})` : ''}</button>
        {(Object.keys(STATUS_LABEL) as ReservationStatus[]).map((s) => (
          <button key={s} className={status === s ? 'tab active' : 'tab'} onClick={() => setStatus(s)}>{STATUS_LABEL[s]}{all ? ` (${all[s] ?? 0})` : ''}</button>
        ))}
      </div>
      <div className="row between">
        <div className="search-box compact"><Search size={16} /><input placeholder="Tìm theo người mượn, sách, mã..." value={kw} onChange={(e) => setKw(e.target.value)} /></div>
        <button className="btn btn-outline" onClick={() => { list.reload(); stats.refresh(); }}><RefreshCw size={15} /> Làm mới</button>
      </div>
      {list.loading ? <Loading /> : list.error ? <ErrorState message={list.error} onRetry={list.reload} /> :
        !rows.length ? <EmptyState text="Không có yêu cầu nào." /> : (
          <div className="table-wrap">
            <table className="table">
              <thead><tr><th>Mã</th><th>Người mượn</th><th>Sách</th><th>Bản sao</th><th>Trạng thái</th><th>Gửi lúc</th><th>Hạn trả</th><th></th></tr></thead>
              <tbody>
                {rows.map((r) => {
                  const left = r.status === 'BORROWED' ? daysUntil(r.dueDate) : null;
                  return (
                    <tr key={r.id}>
                      <td><Link to={`/yeu-cau/${r.id}`}><code>{r.id.slice(0, 8)}</code></Link></td>
                      <td>{r.userName}</td>
                      <td>{r.bookTitle}</td>
                      <td>{r.copyCode ?? '—'}</td>
                      <td><StatusBadge status={r.status} />{r.rejectionReason && <div className="muted small">{r.rejectionReason}</div>}</td>
                      <td className="nowrap">{timeAgo(r.requestedAt)}</td>
                      <td className={left !== null && left < 0 ? 'bad nowrap' : 'nowrap'}>{formatDay(r.dueDate)}{left !== null && left < 0 ? ' (quá hạn)' : ''}</td>
                      <td className="actions">
                        {r.status === 'RESERVED' && <button className="btn" onClick={() => act(() => api.borrow(r.id), 'Đã xác nhận giao sách.')}><BookMarked size={14} /> Giao sách</button>}
                        {r.status === 'BORROWED' && <button className="btn" onClick={() => act(() => api.giveBack(r.id), 'Đã xác nhận trả sách.')}><Undo2 size={14} /> Nhận trả</button>}
                        {(r.status === 'PENDING' || r.status === 'RESERVED') && <button className="btn btn-danger" onClick={() => act(() => api.cancel(r.id), 'Đã hủy yêu cầu.')}><Ban size={14} /> Hủy</button>}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
    </>
  );
}
