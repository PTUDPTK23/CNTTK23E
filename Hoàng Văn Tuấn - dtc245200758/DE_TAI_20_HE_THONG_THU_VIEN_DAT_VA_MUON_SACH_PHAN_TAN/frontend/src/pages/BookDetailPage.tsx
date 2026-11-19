import { BookMarked, ChevronRight, Info, Send } from 'lucide-react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { AvailabilityBar } from '../components/AvailabilityBar';
import { BookCard } from '../components/BookCard';
import { BookCover } from '../components/BookCover';
import { ErrorState, Loading, Notice } from '../components/Feedback';
import { StatusBadge } from '../components/StatusBadge';
import { useAsync } from '../hooks/useAsync';
import { useCurrentUser } from '../hooks/UserContext';
import { api } from '../services/api';
import { COPY_COLOR, COPY_LABEL } from '../utils/format';

export default function BookDetailPage() {
  const { id = '' } = useParams();
  const nav = useNavigate();
  const { current } = useCurrentUser();
  const { data: b, loading, error, reload } = useAsync(() => api.book(id), [id]);
  const copies = useAsync(() => api.copies(id), [id]);
  const mine = useAsync(() => (current ? api.userReservations(current.id) : Promise.resolve([])), [current?.id]);
  const related = useAsync(() => (b ? api.books({ categoryId: b.categoryId, size: 5 }) : Promise.resolve(null)), [b?.categoryId]);

  if (loading) return <Loading />;
  if (error || !b) return <ErrorState message={error ?? 'Không tìm thấy sách'} onRetry={reload} />;
  const ok = b.availableCopies > 0;
  const active = mine.data?.find((r) => r.bookId === b.id && ['PENDING', 'RESERVED', 'BORROWED'].includes(r.status));
  const others = related.data?.items.filter((x) => x.id !== b.id).slice(0, 4) ?? [];

  return (
    <>
      <nav className="crumbs"><Link to="/">Trang chủ</Link><ChevronRight size={14} /><Link to="/sach">Tra cứu sách</Link><ChevronRight size={14} /><span>{b.title}</span></nav>
      <div className="detail-grid">
        <div className="detail-cover"><BookCover title={b.title} author={b.author} src={b.coverImage} /></div>
        <div className="card detail-main">
          <span className="chip">{b.categoryName}</span>
          <h1 className="serif">{b.title}</h1>
          <p className="lead muted">{b.author}</p>
          <p>{b.description ?? 'Chưa có mô tả.'}</p>
          <dl className="meta">
            <div><dt>ISBN</dt><dd>{b.isbn ?? '—'}</dd></div>
            <div><dt>Nhà xuất bản</dt><dd>{b.publisher ?? '—'}</dd></div>
            <div><dt>Năm xuất bản</dt><dd>{b.publicationYear ?? '—'}</dd></div>
            <div><dt>Thể loại</dt><dd>{b.categoryName}</dd></div>
          </dl>
        </div>
        <aside className="card reserve-box">
          <h3>Tình trạng bản sách</h3>
          <div className="avail-big"><b className={ok ? 'ok' : 'bad'}>{b.availableCopies}</b><span className="muted">/ {b.totalCopies} bản khả dụng</span></div>
          <AvailabilityBar total={b.totalCopies} available={b.availableCopies} reserved={b.reservedCopies} borrowed={b.borrowedCopies} showLegend />
          {active ? (
            <Notice kind="info">Bạn đã có yêu cầu cho cuốn này: <StatusBadge status={active.status} /> <Link to={`/yeu-cau/${active.id}`}>Xem chi tiết</Link></Notice>
          ) : !ok ? (
            <Notice kind="warn">Hiện không còn bản khả dụng. Yêu cầu của bạn có thể bị từ chối khi Worker xử lý.</Notice>
          ) : null}
          <button className="btn btn-lg" disabled={Boolean(active)} onClick={() => nav(`/dat-muon/${b.id}`)}>
            <Send size={18} /> Đặt mượn sách
          </button>
          <p className="muted small"><Info size={13} /> Yêu cầu được xử lý bất đồng bộ qua RabbitMQ; kết quả hiển thị ở trang theo dõi.</p>
        </aside>
      </div>

      {copies.data && copies.data.length > 0 && (
        <section className="section">
          <div className="section-head"><h2>Các bản sao</h2></div>
          <div className="copies">
            {copies.data.map((c) => (
              <div key={c.id} className="copy" style={{ ['--c' as string]: COPY_COLOR[c.status] }}>
                <BookMarked size={16} /><b>{c.copyCode}</b><span>{COPY_LABEL[c.status]}</span>
              </div>
            ))}
          </div>
        </section>
      )}

      {others.length > 0 && (
        <section className="section">
          <div className="section-head"><h2>Sách cùng thể loại</h2></div>
          <div className="grid books">{others.map((x) => <BookCard key={x.id} book={x} />)}</div>
        </section>
      )}
    </>
  );
}
