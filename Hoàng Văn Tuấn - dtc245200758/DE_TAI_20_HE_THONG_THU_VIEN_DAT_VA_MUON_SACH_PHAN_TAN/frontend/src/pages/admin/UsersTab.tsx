import { Lock, Plus, Unlock } from 'lucide-react';
import { useState } from 'react';
import { EmptyState, ErrorState, Loading, Notice } from '../../components/Feedback';
import { Modal } from '../../components/Modal';
import { useToast } from '../../hooks/ToastContext';
import { useAsync } from '../../hooks/useAsync';
import { useCurrentUser } from '../../hooks/UserContext';
import { api, ApiError } from '../../services/api';
import type { UserInput } from '../../types';
import { errMsg, formatDay, initials, ROLE_LABEL } from '../../utils/format';

export default function UsersTab() {
  const toast = useToast();
  const { current } = useCurrentUser();
  const users = useAsync(() => api.users(), []);
  const [form, setForm] = useState<UserInput | null>(null);
  const [err, setErr] = useState<string | null>(null);
  const [fieldErr, setFieldErr] = useState<Record<string, string>>({});

  async function save() {
    if (!form) return;
    setErr(null); setFieldErr({});
    try {
      await api.createUser(form);
      toast.push('success', 'Đã tạo người dùng. Tải lại trang để thấy trong danh sách chọn ở góc phải.');
      setForm(null);
      users.reload();
    } catch (e) {
      if (e instanceof ApiError && e.fieldErrors) setFieldErr(e.fieldErrors);
      setErr(errMsg(e));
    }
  }

  async function toggle(id: string, locked: boolean) {
    try { await api.setUserStatus(id, locked ? 'ACTIVE' : 'LOCKED'); toast.push('success', locked ? 'Đã mở khóa tài khoản.' : 'Đã khóa tài khoản.'); users.reload(); }
    catch (e) { toast.push('error', errMsg(e)); }
  }

  const set = (k: keyof UserInput, v: string) => setForm((f) => (f ? { ...f, [k]: v } : f));

  return (
    <>
      <div className="row end"><button className="btn" onClick={() => { setErr(null); setFieldErr({}); setForm({ fullName: '', email: '', phone: '', role: 'USER' }); }}><Plus size={16} /> Thêm người dùng</button></div>
      {users.loading ? <Loading /> : users.error ? <ErrorState message={users.error} onRetry={users.reload} /> :
        !users.data?.length ? <EmptyState text="Chưa có người dùng." /> : (
          <div className="table-wrap">
            <table className="table">
              <thead><tr><th>Người dùng</th><th>Liên hệ</th><th>Vai trò</th><th>Trạng thái</th><th>Ngày tạo</th><th></th></tr></thead>
              <tbody>
                {users.data.map((u) => (
                  <tr key={u.id}>
                    <td><div className="row tight nowrap"><span className="avatar">{initials(u.fullName)}</span><b>{u.fullName}</b></div></td>
                    <td>{u.email}</td>
                    <td><span className="chip">{ROLE_LABEL[u.role]}</span></td>
                    <td><span className={u.status === 'ACTIVE' ? 'pill pill-ok' : 'pill pill-bad'}>{u.status === 'ACTIVE' ? 'Hoạt động' : 'Đã khóa'}</span></td>
                    <td>{formatDay(u.createdAt)}</td>
                    <td className="actions">
                      {u.id !== current?.id && (
                        <button className={u.status === 'ACTIVE' ? 'btn btn-danger' : 'btn btn-outline'} onClick={() => toggle(u.id, u.status !== 'ACTIVE')}>
                          {u.status === 'ACTIVE' ? <><Lock size={14} /> Khóa</> : <><Unlock size={14} /> Mở khóa</>}
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      {form && (
        <Modal title="Thêm người dùng" onClose={() => setForm(null)}>
          {err && <Notice kind="error">{err}</Notice>}
          <div className="form-grid one">
            <label className="field">Họ tên *<input value={form.fullName} onChange={(e) => set('fullName', e.target.value)} />{fieldErr.fullName && <small className="err">{fieldErr.fullName}</small>}</label>
            <label className="field">Email *<input type="email" value={form.email} onChange={(e) => set('email', e.target.value)} />{fieldErr.email && <small className="err">{fieldErr.email}</small>}</label>
            <label className="field">Điện thoại<input value={form.phone ?? ''} onChange={(e) => set('phone', e.target.value)} /></label>
            <label className="field">Vai trò
              <select value={form.role} onChange={(e) => set('role', e.target.value)}>
                {(Object.keys(ROLE_LABEL) as (keyof typeof ROLE_LABEL)[]).map((r) => <option key={r} value={r}>{ROLE_LABEL[r]}</option>)}
              </select>
            </label>
          </div>
          <div className="row end"><button className="btn btn-outline" onClick={() => setForm(null)}>Hủy</button><button className="btn" onClick={save}>Tạo</button></div>
        </Modal>
      )}
    </>
  );
}
