import { AlertTriangle, BookCopy, BookMarked, CheckCircle2, Clock, Hourglass, Library, Users } from 'lucide-react';
import { useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Donut, HBars } from '../../components/Charts';
import { ErrorState, Loading, Notice } from '../../components/Feedback';
import { PipelineFlow } from '../../components/PipelineFlow';
import { StatCard } from '../../components/StatCard';
import { StatusBadge } from '../../components/StatusBadge';
import { useAsync } from '../../hooks/useAsync';
import { api } from '../../services/api';
import type { ReservationStatus } from '../../types';
import { STATUS_COLOR, STATUS_LABEL, timeAgo } from '../../utils/format';

export default function OverviewTab() {
  const stats = useAsync(() => api.stats(), []);
  const cats = useAsync(() => api.statsByCategory(), []);
  const pipe = useAsync(() => api.pipeline(), []);
  const recent = useAsync(() => api.allReservations(), []);

  // Tự làm mới mỗi 5 giây để thấy luồng bất đồng bộ chạy.
  useEffect(() => {
    const t = setInterval(() => { stats.refresh(); cats.refresh(); pipe.refresh(); recent.refresh(); }, 5000);
    return () => clearInterval(t);
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  if (stats.loading) return <Loading />;
  if (stats.error || !stats.data) return <ErrorState message={stats.error ?? 'Lỗi'} onRetry={stats.reload} />;
  const s = stats.data;
  const p = pipe.data;
  const other = Math.max(0, s.totalCopies - s.availableCopies - s.reservedCopies - s.borrowedCopies);
  const statuses = Object.keys(STATUS_LABEL) as ReservationStatus[];

  return (
    <>
      <section className="grid stats">
        <StatCard icon={<Library size={22} />} label="Đầu sách" value={s.totalBooks} tone="primary" />
        <StatCard icon={<BookCopy size={22} />} label="Tổng bản sao" value={s.totalCopies} tone="info" />
        <StatCard icon={<CheckCircle2 size={22} />} label="Còn khả dụng" value={s.availableCopies} tone="ok" />
        <StatCard icon={<BookMarked size={22} />} label="Đang mượn" value={s.borrowedCopies} tone="violet" />
        <StatCard icon={<Hourglass size={22} />} label="Chờ xử lý" value={s.pendingReservations} tone="warn" />
        <StatCard icon={<AlertTriangle size={22} />} label="Mượn quá hạn" value={s.overdueLoans} tone="bad" />
        <StatCard icon={<Users size={22} />} label="Người dùng" value={s.totalUsers} tone="primary" />
      </section>

      <div className="card">
        <div className="row between"><h3>Đường ống bất đồng bộ</h3><span className="muted small">tự làm mới mỗi 5 giây</span></div>
        <PipelineFlow
          badges={p ? {
            api: `Outbox chờ: ${p.outboxPending}`,
            mq: p.rabbitUp ? `${p.queueMessages} message · DLQ ${p.deadLetterMessages}` : 'Không kết nối',
            worker: p.rabbitUp ? (p.consumers > 0 ? `${p.consumers} consumer` : 'Chưa chạy') : '—',
            db: `Đã phát hành: ${p.outboxPublished}`,
          } : undefined}
          tones={p ? {
            api: p.outboxPending > 5 ? 'warn' : 'ok',
            mq: !p.rabbitUp ? 'bad' : p.deadLetterMessages > 0 ? 'warn' : 'ok',
            worker: p.consumers > 0 ? 'ok' : 'bad',
            db: 'ok',
          } : undefined}
        />
        {p && !p.rabbitUp && <Notice kind="error">Không kết nối được RabbitMQ. Yêu cầu mới vẫn được lưu (Outbox) và sẽ gửi khi RabbitMQ hoạt động lại.</Notice>}
        {p && p.rabbitUp && p.consumers === 0 && <Notice kind="warn">Chưa có Reservation Worker nào đang chạy — các yêu cầu sẽ ở trạng thái <b>Đang chờ xử lý</b> cho tới khi Worker khởi động.</Notice>}
        {p && p.deadLetterMessages > 0 && <Notice kind="warn">Có {p.deadLetterMessages} message trong hàng đợi lỗi (DLQ). Kiểm tra RabbitMQ Management (cổng 15672) để xử lý lại.</Notice>}
      </div>

      <div className="grid two">
        <div className="card">
          <h3>Phân bổ bản sao</h3>
          <Donut center={s.totalCopies} sub="bản sao" slices={[
            { label: 'Khả dụng', value: s.availableCopies, color: 'var(--ok)' },
            { label: 'Đã giữ', value: s.reservedCopies, color: 'var(--info)' },
            { label: 'Đang mượn', value: s.borrowedCopies, color: 'var(--violet)' },
            { label: 'Bảo trì / thất lạc', value: other, color: 'var(--muted-2)' },
          ]} />
        </div>
        <div className="card">
          <h3>Yêu cầu theo trạng thái</h3>
          <HBars rows={statuses.map((st) => ({ label: STATUS_LABEL[st], value: s.reservationsByStatus?.[st] ?? 0, color: STATUS_COLOR[st] }))} />
        </div>
      </div>

      <div className="grid two">
        <div className="card">
          <h3>Bản sao theo thể loại</h3>
          {cats.data && cats.data.length > 0
            ? <HBars rows={cats.data.map((c) => ({ label: c.category, value: c.availableCopies, max: Math.max(...cats.data!.map((x) => x.totalCopies)), color: 'var(--primary)', note: `${c.availableCopies}/${c.totalCopies}` }))} />
            : <p className="muted">Chưa có dữ liệu.</p>}
        </div>
        <div className="card">
          <div className="row between"><h3>Hoạt động gần đây</h3><Link to="#" onClick={(e) => e.preventDefault()} className="muted small"><Clock size={13} /> mới nhất</Link></div>
          {recent.data && recent.data.length > 0 ? (
            <ul className="activity">
              {recent.data.slice(0, 6).map((r) => (
                <li key={r.id}>
                  <div className="grow"><b>{r.userName}</b> · {r.bookTitle}<div className="muted small">{timeAgo(r.requestedAt)}</div></div>
                  <StatusBadge status={r.status} />
                </li>
              ))}
            </ul>
          ) : <p className="muted">Chưa có yêu cầu nào.</p>}
        </div>
      </div>
    </>
  );
}
