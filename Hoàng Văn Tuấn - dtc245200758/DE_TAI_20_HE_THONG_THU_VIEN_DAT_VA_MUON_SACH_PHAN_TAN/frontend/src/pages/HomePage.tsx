import { ArrowRight, BookCopy, BookMarked, BookOpen, Clock, Library, Search, ShieldCheck, Sparkles, Zap } from 'lucide-react';
import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { BookCard } from '../components/BookCard';
import { BookGridSkeleton, ErrorState } from '../components/Feedback';
import { PipelineFlow } from '../components/PipelineFlow';
import { StatCard } from '../components/StatCard';
import { useAsync } from '../hooks/useAsync';
import { api } from '../services/api';

const STEPS = [
  { icon: Search, title: '1. Tra cứu', text: 'Tìm sách theo tên, tác giả, thể loại và xem ngay số bản đang khả dụng.' },
  { icon: Zap, title: '2. Gửi yêu cầu', text: 'API tiếp nhận tức thì (HTTP 202), lưu trạng thái Pending và đưa vào RabbitMQ.' },
  { icon: ShieldCheck, title: '3. Worker xử lý', text: 'Worker kiểm tra bản còn trống, giữ sách an toàn (không đặt trùng) hoặc từ chối.' },
  { icon: BookMarked, title: '4. Nhận & trả', text: 'Theo dõi trạng thái Reserved → Borrowed → Returned theo thời gian thực.' },
];

export default function HomePage() {
  const nav = useNavigate();
  const [q, setQ] = useState('');
  const stats = useAsync(() => api.stats(), []);
  const cats = useAsync(() => api.categories(), []);
  const books = useAsync(() => api.books({ page: 0, size: 8, availability: 'AVAILABLE' }), []);
  const s = stats.data;

  function search(e: FormEvent) {
    e.preventDefault();
    nav(q.trim() ? `/sach?q=${encodeURIComponent(q.trim())}` : '/sach');
  }

  return (
    <>
      <section className="hero">
        <div className="hero-bg" aria-hidden />
        <div className="hero-content">
          <span className="eyebrow"><Sparkles size={14} /> Hệ thống thư viện phân tán</span>
          <h1>Tìm sách hay,<br />đặt mượn trong vài giây.</h1>
          <p>Gửi yêu cầu đặt mượn, hệ thống xử lý bất đồng bộ qua RabbitMQ và cập nhật kết quả cho bạn theo thời gian thực.</p>
          <form className="hero-search" onSubmit={search}>
            <Search size={20} />
            <input value={q} onChange={(e) => setQ(e.target.value)} placeholder="Nhập tên sách, tác giả hoặc ISBN..." aria-label="Tìm sách" />
            <button className="btn btn-accent" type="submit">Tra cứu</button>
          </form>
          <div className="hero-links">
            <Link to="/sach"><BookOpen size={15} /> Xem toàn bộ sách</Link>
            <Link to="/yeu-cau"><Clock size={15} /> Theo dõi yêu cầu</Link>
          </div>
        </div>
        <div className="hero-shelf" aria-hidden>
          {[0, 1, 2, 3, 4, 5].map((i) => <span key={i} className={`spine spine-${i}`} />)}
        </div>
      </section>

      {stats.error ? <ErrorState message={stats.error} onRetry={stats.reload} /> : (
        <section className="grid stats">
          <StatCard icon={<Library size={22} />} label="Đầu sách" value={s?.totalBooks ?? 0} tone="primary" />
          <StatCard icon={<BookCopy size={22} />} label="Tổng số bản sách" value={s?.totalCopies ?? 0} tone="info" />
          <StatCard icon={<BookOpen size={22} />} label="Đang khả dụng" value={s?.availableCopies ?? 0} tone="ok" />
          <StatCard icon={<BookMarked size={22} />} label="Đang giữ / mượn" value={(s?.borrowedCopies ?? 0) + (s?.reservedCopies ?? 0)} tone="violet" />
        </section>
      )}

      <section className="section">
        <div className="section-head"><h2>Cách hệ thống hoạt động</h2><p className="muted">Yêu cầu đặt mượn được xử lý bất đồng bộ, không chặn người dùng.</p></div>
        <div className="card"><PipelineFlow /></div>
        <div className="grid steps">
          {STEPS.map(({ icon: I, title, text }) => (
            <div key={title} className="step-card"><span className="step-ico"><I size={20} /></span><h3>{title}</h3><p className="muted">{text}</p></div>
          ))}
        </div>
      </section>

      {cats.data && cats.data.length > 0 && (
        <section className="section">
          <div className="section-head"><h2>Khám phá theo thể loại</h2></div>
          <div className="cat-chips">
            {cats.data.map((c) => <Link key={c.id} to={`/sach?categoryId=${c.id}`} className="cat-chip"><b>{c.name}</b><span className="muted small">{c.description}</span></Link>)}
          </div>
        </section>
      )}

      <section className="section">
        <div className="section-head row between">
          <div><h2>Sách đang có sẵn</h2><p className="muted">Những đầu sách bạn có thể đặt mượn ngay.</p></div>
          <Link to="/sach" className="btn btn-outline">Xem tất cả <ArrowRight size={16} /></Link>
        </div>
        {books.loading ? <BookGridSkeleton /> : books.error ? <ErrorState message={books.error} onRetry={books.reload} /> : (
          <div className="grid books">{books.data?.items.map((b) => <BookCard key={b.id} book={b} />)}</div>
        )}
      </section>
    </>
  );
}
