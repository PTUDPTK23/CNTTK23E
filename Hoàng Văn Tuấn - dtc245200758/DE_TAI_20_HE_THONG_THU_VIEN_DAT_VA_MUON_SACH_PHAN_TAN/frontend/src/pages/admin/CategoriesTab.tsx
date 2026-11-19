import { Pencil, Plus } from 'lucide-react';
import { useState } from 'react';
import { EmptyState, ErrorState, Loading, Notice } from '../../components/Feedback';
import { Modal } from '../../components/Modal';
import { useToast } from '../../hooks/ToastContext';
import { useAsync } from '../../hooks/useAsync';
import { api } from '../../services/api';
import { errMsg } from '../../utils/format';

export default function CategoriesTab() {
  const toast = useToast();
  const cats = useAsync(() => api.categories(), []);
  const stats = useAsync(() => api.statsByCategory(), []);
  const [form, setForm] = useState<{ id: string | null; name: string; description: string } | null>(null);
  const [err, setErr] = useState<string | null>(null);

  async function save() {
    if (!form) return;
    setErr(null);
    try {
      const body = { name: form.name, description: form.description };
      if (form.id) await api.updateCategory(form.id, body); else await api.createCategory(body);
      toast.push('success', form.id ? 'Đã cập nhật thể loại.' : 'Đã thêm thể loại.');
      setForm(null);
      cats.reload();
    } catch (e) { setErr(errMsg(e)); }
  }

  return (
    <>
      <div className="row end"><button className="btn" onClick={() => { setErr(null); setForm({ id: null, name: '', description: '' }); }}><Plus size={16} /> Thêm thể loại</button></div>
      {cats.loading ? <Loading /> : cats.error ? <ErrorState message={cats.error} onRetry={cats.reload} /> :
        !cats.data?.length ? <EmptyState text="Chưa có thể loại." /> : (
          <div className="grid cats">
            {cats.data.map((c) => {
              const st = stats.data?.find((x) => x.category === c.name);
              return (
                <div key={c.id} className="card cat-card">
                  <div className="row between"><h3>{c.name}</h3>
                    <button className="icon-btn" aria-label="Sửa" onClick={() => { setErr(null); setForm({ id: c.id, name: c.name, description: c.description ?? '' }); }}><Pencil size={16} /></button></div>
                  <p className="muted small">{c.description ?? 'Chưa có mô tả.'}</p>
                  <span className="chip">{st ? `${st.availableCopies}/${st.totalCopies} bản khả dụng` : '0 bản sao'}</span>
                </div>
              );
            })}
          </div>
        )}
      {form && (
        <Modal title={form.id ? 'Sửa thể loại' : 'Thêm thể loại'} onClose={() => setForm(null)}>
          {err && <Notice kind="error">{err}</Notice>}
          <div className="form-grid one">
            <label className="field">Tên thể loại *<input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></label>
            <label className="field">Mô tả<textarea rows={3} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} /></label>
          </div>
          <div className="row end"><button className="btn btn-outline" onClick={() => setForm(null)}>Hủy</button><button className="btn" onClick={save}>Lưu</button></div>
        </Modal>
      )}
    </>
  );
}
