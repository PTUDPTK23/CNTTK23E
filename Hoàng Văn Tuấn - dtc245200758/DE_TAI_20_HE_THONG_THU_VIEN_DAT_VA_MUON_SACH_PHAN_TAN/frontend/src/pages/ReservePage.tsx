import { ArrowRight, Check, Send } from 'lucide-react';
import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { AvailabilityBar } from '../components/AvailabilityBar';
import { BookCover } from '../components/BookCover';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { ErrorState, Loading, Notice } from '../components/Feedback';
import { useToast } from '../hooks/ToastContext';
import { useAsync } from '../hooks/useAsync';
import { useCurrentUser } from '../hooks/UserContext';
import { api } from '../services/api';
import { errMsg, initials, ROLE_LABEL } from '../utils/format';

export default function ReservePage() {
  const { bookId = '' } = useParams();
  const nav = useNavigate();
  const toast = useToast();
  const { current } = useCurrentUser();
  const { data: b, loading, error, reload } = useAsync(() => api.book(bookId), [bookId]);
  const [confirm, setConfirm] = useState(false);
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  async function submit() {
    if (!current || !b) return;
    setConfirm(false);
    setBusy(true);
    setErr(null);
    try {
      const created = await api.createReservation(current.id, b.id);
      toast.push('success', 'Đã gửi yêu cầu. Hệ thống đang xử lý bất đồng bộ.');
      nav(`/yeu-cau/${created.reservationId}`, { state: { justCreated: true } });
    } catch (e) {
      setErr(errMsg(e));
      setBusy(false);
    }
  }

  if (loading) return <Loading />;
  if (error || !b) return <ErrorState message={error ?? 'Không tìm thấy sách'} onRetry={reload} />;
  return (
    <>
      <p><Link to={`/sach/${b.id}`}>← Quay lại chi tiết sách</Link></p>
      <div className="page-head"><h1>Đặt mượn sách</h1><p className="muted">Kiểm tra thông tin và gửi yêu cầu. Bạn sẽ được chuyển tới trang theo dõi trạng thái.</p></div>
      <div className="reserve-layout">
        <div className="card reserve-book">
          <BookCover title={b.title} author={b.author} src={b.coverImage} className="mini-cover" />
          <div className="grow">
            <span className="chip">{b.categoryName}</span>
            <h2 className="serif">{b.title}</h2>
            <p className="muted">{b.author}</p>
            <AvailabilityBar total={b.totalCopies} available={b.availableCopies} reserved={b.reservedCopies} borrowed={b.borrowedCopies} showLegend />
          </div>
        </div>
        <div className="card">
          <h3>Người mượn</h3>
          {current ? (
            <div className="person"><span className="avatar lg">{initials(current.fullName)}</span><div><b>{current.fullName}</b><div className="muted small">{current.email} · {ROLE_LABEL[current.role]}</div></div></div>
          ) : <Notice kind="warn">Chưa chọn người dùng. Hãy chọn ở góc phải phía trên.</Notice>}
          <ol className="mini-steps">
            <li><Check size={14} /> Gửi yêu cầu → trạng thái <b>Đang chờ xử lý</b></li>
            <li><Check size={14} /> Worker kiểm tra số bản còn trống qua RabbitMQ</li>
            <li><Check size={14} /> Nhận kết quả <b>Đã giữ sách</b> hoặc <b>Bị từ chối</b></li>
          </ol>
          {b.availableCopies === 0 && <Notice kind="warn">Hiện không còn bản khả dụng — yêu cầu có thể bị từ chối.</Notice>}
          {err && <Notice kind="error">{err}</Notice>}
          <button className="btn btn-lg" disabled={!current || busy} onClick={() => setConfirm(true)}>
            <Send size={18} /> {busy ? 'Đang gửi...' : 'Gửi yêu cầu đặt mượn'} {!busy && <ArrowRight size={16} />}
          </button>
        </div>
      </div>
      <ConfirmDialog open={confirm} title="Xác nhận đặt mượn" message={`Gửi yêu cầu đặt mượn "${b.title}"?`}
        confirmText="Gửi yêu cầu" onConfirm={submit} onCancel={() => setConfirm(false)} />
    </>
  );
}
