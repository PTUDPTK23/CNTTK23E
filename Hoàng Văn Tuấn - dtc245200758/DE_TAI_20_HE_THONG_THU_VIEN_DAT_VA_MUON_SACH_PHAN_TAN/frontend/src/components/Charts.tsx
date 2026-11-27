export interface Slice { label: string; value: number; color: string }

/** Biểu đồ tròn (donut) bằng SVG thuần, không cần thư viện ngoài. */
export function Donut({ slices, center, sub }: { slices: Slice[]; center: string | number; sub: string }) {
  const total = slices.reduce((s, x) => s + x.value, 0);
  const R = 52, C = 2 * Math.PI * R;
  let offset = 0;
  return (
    <div className="donut">
      <svg viewBox="0 0 140 140" width="160" height="160" role="img" aria-label={sub}>
        <circle cx="70" cy="70" r={R} fill="none" stroke="var(--line)" strokeWidth="16" />
        {total > 0 && slices.filter((s) => s.value > 0).map((s) => {
          const len = (s.value / total) * C;
          const el = (
            <circle key={s.label} cx="70" cy="70" r={R} fill="none" stroke={s.color} strokeWidth="16"
              strokeDasharray={`${len} ${C - len}`} strokeDashoffset={-offset} transform="rotate(-90 70 70)" />
          );
          offset += len;
          return el;
        })}
        <text x="70" y="68" textAnchor="middle" className="donut-num">{center}</text>
        <text x="70" y="86" textAnchor="middle" className="donut-sub">{sub}</text>
      </svg>
      <ul className="legend-list">
        {slices.map((s) => <li key={s.label}><i style={{ background: s.color }} />{s.label}<b>{s.value}</b></li>)}
      </ul>
    </div>
  );
}

/** Biểu đồ cột ngang. */
export function HBars({ rows }: { rows: { label: string; value: number; max?: number; color?: string; note?: string }[] }) {
  const max = Math.max(1, ...rows.map((r) => r.max ?? r.value));
  return (
    <div className="hbars">
      {rows.map((r) => (
        <div key={r.label} className="hbar">
          <span className="hbar-label">{r.label}</span>
          <div className="hbar-track"><div className="hbar-fill" style={{ width: `${(r.value / max) * 100}%`, background: r.color ?? 'var(--primary)' }} /></div>
          <span className="hbar-val">{r.note ?? r.value}</span>
        </div>
      ))}
    </div>
  );
}
