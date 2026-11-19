import { CheckCircle2, Hourglass, BookMarked, Send, Undo2, XCircle, Ban } from 'lucide-react';
import type { Reservation } from '../types';

/** Tiến trình xử lý đặt mượn: Gửi yêu cầu -> Worker xử lý -> Đã giữ -> Đang mượn -> Đã trả. */
export function LifecycleStepper({ r }: { r: Reservation }) {
  const s = r.status;
  const failed = s === 'REJECTED' || s === 'CANCELLED';
  // chỉ số bước hiện tại (0..4)
  const cur = s === 'PENDING' ? 1 : s === 'RESERVED' ? 2 : s === 'BORROWED' ? 3 : s === 'RETURNED' ? 4 : r.processedAt ? 2 : 1;
  const steps = [
    { label: 'Gửi yêu cầu', note: 'API ghi PENDING + Outbox', icon: Send },
    { label: 'Worker xử lý', note: 'Qua RabbitMQ', icon: Hourglass },
    { label: s === 'REJECTED' ? 'Bị từ chối' : 'Đã giữ sách', note: s === 'REJECTED' ? 'Hết bản / không hợp lệ' : 'Chờ nhận tại quầy', icon: s === 'REJECTED' ? XCircle : CheckCircle2 },
    { label: 'Đang mượn', note: 'Thủ thư giao sách', icon: BookMarked },
    { label: 'Đã trả', note: 'Hoàn tất', icon: Undo2 },
  ];
  return (
    <ol className="stepper">
      {steps.map((st, i) => {
        let cls = 'todo';
        if (failed && s === 'REJECTED') cls = i < 2 ? 'done' : i === 2 ? 'fail' : 'todo';
        else if (s === 'CANCELLED') cls = i < cur ? 'done' : i === cur ? 'fail' : 'todo';
        else cls = i < cur ? 'done' : i === cur ? (s === 'PENDING' ? 'active' : 'done') : 'todo';
        if (s === 'RETURNED') cls = 'done';
        const I = s === 'CANCELLED' && cls === 'fail' ? Ban : st.icon;
        return (
          <li key={st.label} className={`step step-${cls}`}>
            <span className="step-dot"><I size={16} /></span>
            <b>{s === 'CANCELLED' && cls === 'fail' ? 'Đã hủy' : st.label}</b>
            <small className="muted">{st.note}</small>
          </li>
        );
      })}
    </ol>
  );
}
