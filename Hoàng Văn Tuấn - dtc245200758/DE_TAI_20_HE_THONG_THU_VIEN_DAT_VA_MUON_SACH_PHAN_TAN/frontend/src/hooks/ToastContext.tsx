import { createContext, useCallback, useContext, useMemo, useRef, useState, type ReactNode } from 'react';
import { AlertTriangle, CheckCircle2, Info, X } from 'lucide-react';

type Kind = 'success' | 'error' | 'info';
interface Toast { id: number; kind: Kind; text: string }
interface Ctx { push: (kind: Kind, text: string) => void }

const ToastCtx = createContext<Ctx>({ push: () => {} });

/** Thông báo nổi (toast) dùng chung toàn ứng dụng. */
export function ToastProvider({ children }: { children: ReactNode }) {
  const [items, setItems] = useState<Toast[]>([]);
  const seq = useRef(0);
  const remove = useCallback((id: number) => setItems((l) => l.filter((t) => t.id !== id)), []);
  const push = useCallback((kind: Kind, text: string) => {
    const id = ++seq.current;
    setItems((l) => [...l.slice(-3), { id, kind, text }]);
    setTimeout(() => remove(id), kind === 'error' ? 6000 : 3500);
  }, [remove]);
  const value = useMemo(() => ({ push }), [push]);
  const Icon = { success: CheckCircle2, error: AlertTriangle, info: Info };
  return (
    <ToastCtx.Provider value={value}>
      {children}
      <div className="toasts" aria-live="polite">
        {items.map((t) => {
          const I = Icon[t.kind];
          return (
            <div key={t.id} className={`toast toast-${t.kind}`} role={t.kind === 'error' ? 'alert' : 'status'}>
              <I size={18} /> <span>{t.text}</span>
              <button className="icon-btn" aria-label="Đóng" onClick={() => remove(t.id)}><X size={14} /></button>
            </div>
          );
        })}
      </div>
    </ToastCtx.Provider>
  );
}

export const useToast = () => useContext(ToastCtx);
