import { LayoutGrid, List, Search, X } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { BookCard, BookRow } from '../components/BookCard';
import { BookGridSkeleton, EmptyState, ErrorState } from '../components/Feedback';
import { Pagination } from '../components/Pagination';
import { useAsync } from '../hooks/useAsync';
import { api } from '../services/api';

const AVAIL = [
  { v: '', label: 'Tất cả' },
  { v: 'AVAILABLE', label: 'Còn sách' },
  { v: 'UNAVAILABLE', label: 'Hết sách' },
];

export default function BooksPage() {
  const [params, setParams] = useSearchParams();
  const [keyword, setKeyword] = useState(params.get('q') ?? '');
  const [debounced, setDebounced] = useState(keyword);
  const categoryId = params.get('categoryId') ?? '';
  const availability = params.get('availability') ?? '';
  const [author, setAuthor] = useState('');
  const [page, setPage] = useState(0);
  const [view, setView] = useState<'grid' | 'list'>('grid');

  useEffect(() => {
    const t = setTimeout(() => { setDebounced(keyword); setPage(0); }, 300);
    return () => clearTimeout(t);
  }, [keyword]);

  const cats = useAsync(() => api.categories(), []);
  const books = useAsync(
    () => api.books({ page, size: 12, keyword: debounced, categoryId, author, availability }),
    [page, debounced, categoryId, author, availability],
  );

  function setFilter(key: string, value: string) {
    const next = new URLSearchParams(params);
    if (value) next.set(key, value); else next.delete(key);
    setParams(next, { replace: true });
    setPage(0);
  }
  const hasFilter = Boolean(keyword || categoryId || availability || author);
  function clear() { setKeyword(''); setAuthor(''); setParams({}, { replace: true }); setPage(0); }

  return (
    <>
      <div className="page-head">
        <h1>Tra cứu sách</h1>
        <p className="muted">Tìm theo tên sách, tác giả, ISBN; lọc theo thể loại và tình trạng khả dụng.</p>
      </div>

      <div className="card filters-card">
        <div className="search-box">
          <Search size={18} />
          <input placeholder="Tìm theo tên sách, tác giả, ISBN..." value={keyword} onChange={(e) => setKeyword(e.target.value)} aria-label="Từ khóa" />
          {keyword && <button className="icon-btn" aria-label="Xóa từ khóa" onClick={() => setKeyword('')}><X size={16} /></button>}
        </div>
        <input className="author-input" placeholder="Lọc theo tác giả" value={author} onChange={(e) => { setAuthor(e.target.value); setPage(0); }} aria-label="Tác giả" />
        <div className="segmented" role="group" aria-label="Tình trạng">
          {AVAIL.map((a) => <button key={a.v} className={availability === a.v ? 'seg active' : 'seg'} onClick={() => setFilter('availability', a.v)}>{a.label}</button>)}
        </div>
        <div className="cat-filter">
          <button className={categoryId === '' ? 'tab active' : 'tab'} onClick={() => setFilter('categoryId', '')}>Mọi thể loại</button>
          {cats.data?.map((c) => <button key={c.id} className={categoryId === c.id ? 'tab active' : 'tab'} onClick={() => setFilter('categoryId', c.id)}>{c.name}</button>)}
        </div>
      </div>

      <div className="row between results-bar">
        <span className="muted">{books.data ? `Tìm thấy ${books.data.totalElements} đầu sách` : ' '}</span>
        <div className="row">
          {hasFilter && <button className="btn btn-ghost" onClick={clear}><X size={14} /> Xóa bộ lọc</button>}
          <div className="segmented">
            <button className={view === 'grid' ? 'seg active' : 'seg'} aria-label="Dạng lưới" onClick={() => setView('grid')}><LayoutGrid size={16} /></button>
            <button className={view === 'list' ? 'seg active' : 'seg'} aria-label="Dạng danh sách" onClick={() => setView('list')}><List size={16} /></button>
          </div>
        </div>
      </div>

      {books.loading ? <BookGridSkeleton n={8} /> : books.error ? <ErrorState message={books.error} onRetry={books.reload} /> :
        !books.data || books.data.items.length === 0 ? <EmptyState text="Không tìm thấy sách phù hợp.">{hasFilter && <button className="btn btn-outline" onClick={clear}>Xóa bộ lọc</button>}</EmptyState> : (
          <>
            {view === 'grid'
              ? <div className="grid books">{books.data.items.map((b) => <BookCard key={b.id} book={b} />)}</div>
              : <div className="list">{books.data.items.map((b) => <BookRow key={b.id} book={b} />)}</div>}
            <Pagination page={books.data.page} totalPages={books.data.totalPages} onChange={setPage} />
          </>
        )}
    </>
  );
}
