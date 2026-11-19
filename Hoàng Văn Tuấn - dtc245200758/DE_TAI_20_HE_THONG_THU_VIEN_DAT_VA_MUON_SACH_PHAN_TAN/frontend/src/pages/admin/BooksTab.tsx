import { Copy as CopyIcon, Pencil, Plus, Search, Trash2 } from 'lucide-react';
import { useEffect, useState } from 'react';
import { BookCover } from '../../components/BookCover';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { EmptyState, ErrorState, Loading, Notice } from '../../components/Feedback';
import { Modal } from '../../components/Modal';
import { Pagination } from '../../components/Pagination';
import { useToast } from '../../hooks/ToastContext';
import { useAsync } from '../../hooks/useAsync';
import { api, ApiError } from '../../services/api';
import type { Book, BookInput, CopyStatus } from '../../types';
import { COPY_COLOR, COPY_LABEL, errMsg } from '../../utils/format';

const EMPTY: BookInput = { title: '', author: '', isbn: '', publisher: '', publicationYear: null, categoryId: '', description: '', coverImage: '' };

export default function BooksTab() {
  const toast = useToast();
  const cats = useAsync(() => api.categories(), []);
  const [kw, setKw] = useState('');
  const [debounced, setDebounced] = useState('');
  const [page, setPage] = useState(0);
  const books = useAsync(() => api.books({ page, size: 10, keyword: debounced }), [page, debounced]);
  const [form, setForm] = useState<{ id: string | null; data: BookInput } | null>(null);
  const [fieldErr, setFieldErr] = useState<Record<string, string>>({});
  const [formErr, setFormErr] = useState<string | null>(null);
  const [toDelete, setToDelete] = useState<Book | null>(null);
  const [copiesOf, setCopiesOf] = useState<Book | null>(null);

  useEffect(() => {
    const t = setTimeout(() => { setDebounced(kw); setPage(0); }, 300);
    return () => clearTimeout(t);
  }, [kw]);

  function openNew() { setFieldErr({}); setFormErr(null); setForm({ id: null, data: { ...EMPTY, categoryId: cats.data?.[0]?.id ?? '' } }); }
  function openEdit(b: Book) {
    setFieldErr({}); setFormErr(null);
    setForm({ id: b.id, data: { title: b.title, author: b.author, isbn: b.isbn ?? '', publisher: b.publisher ?? '', publicationYear: b.publicationYear, categoryId: b.categoryId, description: b.description ?? '', coverImage: b.coverImage ?? '' } });
  }

  async function save() {
    if (!form) return;
    setFieldErr({}); setFormErr(null);
    try {
      const d = form.data;
      const payload: BookInput = { ...d, publicationYear: d.publicationYear ? Number(d.publicationYear) : null };
      if (form.id) await api.updateBook(form.id, payload); else await api.createBook(payload);
      toast.push('success', form.id ? 'Đã cập nhật sách.' : 'Đã thêm sách.');
      setForm(null);
      books.reload();
    } catch (e) {
      if (e instanceof ApiError && e.fieldErrors) setFieldErr(e.fieldErrors);
      setFormErr(errMsg(e));
    }
  }

  async function remove() {
    const b = toDelete;
    setToDelete(null);
    if (!b) return;
    try { await api.deleteBook(b.id); toast.push('success', 'Đã xóa sách.'); books.reload(); }
    catch (e) { toast.push('error', errMsg(e)); }
  }

  const set = (k: keyof BookInput, v: string) => setForm((f) => (f ? { ...f, data: { ...f.data, [k]: v } } : f));
  const field = (k: keyof BookInput, label: string, type = 'text') => (
    <label className="field">{label}
      <input type={type} value={(form?.data[k] as string | number | null | undefined) ?? ''} onChange={(e) => set(k, e.target.value)} />
      {fieldErr[k] && <small className="err">{fieldErr[k]}</small>}
    </label>
  );

  return (
    <>
      <div className="row between">
        <div className="search-box compact"><Search size={16} /><input placeholder="Tìm sách..." value={kw} onChange={(e) => setKw(e.target.value)} /></div>
        <button className="btn" onClick={openNew}><Plus size={16} /> Thêm sách</button>
      </div>

      {books.loading ? <Loading /> : books.error ? <ErrorState message={books.error} onRetry={books.reload} /> :
        !books.data?.items.length ? <EmptyState text="Không có sách." /> : (
          <>
            <div className="table-wrap">
              <table className="table">
                <thead><tr><th>Sách</th><th>Thể loại</th><th>Khả dụng</th><th>Giữ / Mượn</th><th></th></tr></thead>
                <tbody>
                  {books.data.items.map((b) => (
                    <tr key={b.id}>
                      <td><div className="row tight nowrap"><BookCover title={b.title} src={b.coverImage} className="thumb" /><div><b>{b.title}</b><div className="muted small">{b.author}</div></div></div></td>
                      <td><span className="chip">{b.categoryName}</span></td>
                      <td><b className={b.availableCopies > 0 ? 'ok' : 'bad'}>{b.availableCopies}</b> / {b.totalCopies}</td>
                      <td>{b.reservedCopies} / {b.borrowedCopies}</td>
                      <td className="actions">
                        <button className="btn btn-outline" onClick={() => setCopiesOf(b)}><CopyIcon size={14} /> Bản sao</button>
                        <button className="btn btn-outline" onClick={() => openEdit(b)}><Pencil size={14} /> Sửa</button>
                        <button className="btn btn-danger" onClick={() => setToDelete(b)}><Trash2 size={14} /> Xóa</button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <Pagination page={books.data.page} totalPages={books.data.totalPages} onChange={setPage} />
          </>
        )}

      {form && (
        <Modal title={form.id ? 'Sửa sách' : 'Thêm sách'} wide onClose={() => setForm(null)}>
          {formErr && <Notice kind="error">{formErr}</Notice>}
          <div className="form-grid">
            {field('title', 'Tên sách *')}
            {field('author', 'Tác giả *')}
            {field('isbn', 'ISBN')}
            {field('publisher', 'Nhà xuất bản')}
            {field('publicationYear', 'Năm xuất bản', 'number')}
            <label className="field">Thể loại *
              <select value={form.data.categoryId} onChange={(e) => set('categoryId', e.target.value)}>
                {cats.data?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
              {fieldErr.categoryId && <small className="err">{fieldErr.categoryId}</small>}
            </label>
            {field('coverImage', 'URL ảnh bìa')}
            <label className="field wide">Mô tả
              <textarea rows={3} value={form.data.description ?? ''} onChange={(e) => set('description', e.target.value)} />
            </label>
          </div>
          <div className="row end">
            <button className="btn btn-outline" onClick={() => setForm(null)}>Hủy</button>
            <button className="btn" onClick={save}>Lưu</button>
          </div>
        </Modal>
      )}
      {copiesOf && <CopiesDialog book={copiesOf} onClose={() => { setCopiesOf(null); books.reload(); }} />}
      <ConfirmDialog open={toDelete !== null} danger title="Xóa sách" message={`Xóa "${toDelete?.title}"? Lịch sử đặt mượn vẫn được giữ.`}
        confirmText="Xóa" onConfirm={remove} onCancel={() => setToDelete(null)} />
    </>
  );
}

function CopiesDialog({ book, onClose }: { book: Book; onClose: () => void }) {
  const toast = useToast();
  const copies = useAsync(() => api.copies(book.id), [book.id]);

  async function run(fn: () => Promise<unknown>, ok: string) {
    try { await fn(); toast.push('success', ok); copies.reload(); }
    catch (e) { toast.push('error', errMsg(e)); }
  }

  return (
    <Modal title={`Bản sao: ${book.title}`} wide onClose={onClose}>
      {copies.loading ? <Loading /> : copies.error ? <ErrorState message={copies.error} onRetry={copies.reload} /> :
        !copies.data?.length ? <EmptyState text="Chưa có bản sao nào." /> : (
          <table className="table">
            <thead><tr><th>Mã bản sao</th><th>Trạng thái</th></tr></thead>
            <tbody>
              {copies.data.map((c) => (
                <tr key={c.id}>
                  <td><b>{c.copyCode}</b></td>
                  <td>
                    {c.status === 'RESERVED' || c.status === 'BORROWED' ? (
                      <span className="badge" style={{ ['--c' as string]: COPY_COLOR[c.status] }}>{COPY_LABEL[c.status]}</span>
                    ) : (
                      <select value={c.status} onChange={(e) => run(() => api.setCopyStatus(c.id, e.target.value as CopyStatus), 'Đã cập nhật trạng thái bản sao.')}>
                        {(['AVAILABLE', 'MAINTENANCE', 'LOST'] as CopyStatus[]).map((s) => <option key={s} value={s}>{COPY_LABEL[s]}</option>)}
                      </select>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      <div className="row between">
        <button className="btn" onClick={() => run(() => api.addCopy(book.id), 'Đã thêm bản sao.')}><Plus size={16} /> Thêm bản sao</button>
        <button className="btn btn-outline" onClick={onClose}>Đóng</button>
      </div>
    </Modal>
  );
}
