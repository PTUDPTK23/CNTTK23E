import type { CopyStatus, ReservationStatus } from '../types';

export const STATUS_LABEL: Record<ReservationStatus, string> = {
  PENDING: 'Đang chờ xử lý',
  RESERVED: 'Đã giữ sách',
  REJECTED: 'Bị từ chối',
  BORROWED: 'Đang mượn',
  RETURNED: 'Đã trả',
  CANCELLED: 'Đã hủy',
};

export const STATUS_COLOR: Record<ReservationStatus, string> = {
  PENDING: '#d97706',
  RESERVED: '#2563eb',
  REJECTED: '#dc2626',
  BORROWED: '#7c3aed',
  RETURNED: '#16a34a',
  CANCELLED: '#6b7280',
};

export const COPY_LABEL: Record<CopyStatus, string> = {
  AVAILABLE: 'Sẵn sàng',
  RESERVED: 'Đã giữ',
  BORROWED: 'Đang mượn',
  MAINTENANCE: 'Bảo trì',
  LOST: 'Thất lạc',
};

export const COPY_COLOR: Record<CopyStatus, string> = {
  AVAILABLE: '#16a34a',
  RESERVED: '#2563eb',
  BORROWED: '#7c3aed',
  MAINTENANCE: '#d97706',
  LOST: '#dc2626',
};

export const ROLE_LABEL = { ADMIN: 'Quản trị viên', LIBRARIAN: 'Thủ thư', USER: 'Bạn đọc' } as const;

export function formatDate(s: string | null | undefined): string {
  return s ? new Date(s).toLocaleString('vi-VN') : '—';
}

export function formatDay(s: string | null | undefined): string {
  return s ? new Date(s).toLocaleDateString('vi-VN') : '—';
}

/** "3 phút trước", "2 giờ trước"... */
export function timeAgo(s: string | null | undefined): string {
  if (!s) return '—';
  const sec = Math.max(0, Math.floor((Date.now() - new Date(s).getTime()) / 1000));
  if (sec < 45) return 'vừa xong';
  if (sec < 3600) return `${Math.max(1, Math.round(sec / 60))} phút trước`;
  if (sec < 86400) return `${Math.round(sec / 3600)} giờ trước`;
  return `${Math.round(sec / 86400)} ngày trước`;
}

/** Số ngày còn lại tới hạn trả (âm = quá hạn). */
export function daysUntil(s: string | null | undefined): number | null {
  if (!s) return null;
  return Math.ceil((new Date(s).getTime() - Date.now()) / 86400000);
}

export function initials(name: string): string {
  const parts = name.trim().split(/\s+/);
  return ((parts[0]?.[0] ?? '') + (parts.length > 1 ? parts[parts.length - 1][0] : '')).toUpperCase();
}

export function errMsg(e: unknown): string {
  return e instanceof Error ? e.message : String(e);
}
