import { AlertTriangle, CheckCircle2, Info, Inbox, Loader2 } from 'lucide-react';
import type { ReactNode } from 'react';

export function Loading({ text = 'Đang tải...' }: { text?: string }) {
  return <div className="state"><Loader2 className="spin" size={22} /> <span>{text}</span></div>;
}

export function ErrorState({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <div className="state state-error" role="alert">
      <AlertTriangle size={22} />
      <span>{message}</span>
      {onRetry && <button className="btn btn-outline" onClick={onRetry}>Thử lại</button>}
    </div>
  );
}

export function EmptyState({ text, children }: { text: string; children?: ReactNode }) {
  return <div className="state empty"><Inbox size={30} /> <span>{text}</span>{children}</div>;
}

export function Notice({ kind, children }: { kind: 'success' | 'error' | 'info' | 'warn'; children: ReactNode }) {
  const I = kind === 'success' ? CheckCircle2 : kind === 'info' ? Info : AlertTriangle;
  return <div className={`notice notice-${kind}`} role={kind === 'error' ? 'alert' : 'status'}><I size={18} /><div>{children}</div></div>;
}

/** Khung chờ (skeleton) cho lưới sách. */
export function BookGridSkeleton({ n = 8 }: { n?: number }) {
  return (
    <div className="grid books">
      {Array.from({ length: n }).map((_, i) => (
        <div key={i} className="book-card skeleton-card">
          <div className="skeleton" style={{ aspectRatio: '3/4' }} />
          <div className="book-info"><div className="skeleton sk-line" /><div className="skeleton sk-line w60" /><div className="skeleton sk-line w40" /></div>
        </div>
      ))}
    </div>
  );
}
