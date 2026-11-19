import { Ban, BookMarked, CheckCircle2, Hourglass, Undo2, XCircle } from 'lucide-react';
import type { ReservationStatus } from '../types';
import { STATUS_COLOR, STATUS_LABEL } from '../utils/format';

const ICON = { PENDING: Hourglass, RESERVED: CheckCircle2, REJECTED: XCircle, BORROWED: BookMarked, RETURNED: Undo2, CANCELLED: Ban };

export function StatusBadge({ status }: { status: ReservationStatus }) {
  const I = ICON[status];
  return (
    <span className={`badge badge-${status.toLowerCase()}`} style={{ ['--c' as string]: STATUS_COLOR[status] }}>
      <I size={13} /> {STATUS_LABEL[status]}
    </span>
  );
}
