import type { ReactNode } from 'react';
import { useCountUp } from '../hooks/useCountUp';

export function StatCard({ icon, label, value, tone = 'primary', hint }: { icon: ReactNode; label: string; value: number; tone?: string; hint?: string }) {
  const v = useCountUp(value);
  return (
    <div className="stat-card" style={{ ['--tone' as string]: `var(--${tone})` }}>
      <div className="stat-icon">{icon}</div>
      <div>
        <b>{v}</b>
        <span>{label}</span>
        {hint && <small className="muted">{hint}</small>}
      </div>
    </div>
  );
}
