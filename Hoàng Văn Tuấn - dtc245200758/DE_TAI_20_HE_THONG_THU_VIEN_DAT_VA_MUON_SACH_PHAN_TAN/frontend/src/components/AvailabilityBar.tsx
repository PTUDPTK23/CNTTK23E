/** Thanh phân bổ số bản: khả dụng / đã giữ / đang mượn / khác. */
export function AvailabilityBar({ total, available, reserved, borrowed, showLegend = false }:
  { total: number; available: number; reserved: number; borrowed: number; showLegend?: boolean }) {
  const other = Math.max(0, total - available - reserved - borrowed);
  const pct = (n: number) => (total === 0 ? 0 : (n / total) * 100);
  return (
    <div className="avail-wrap">
      <div className="avail-bar" role="img" aria-label={`${available} trên ${total} bản khả dụng`}>
        <span style={{ width: `${pct(available)}%`, background: 'var(--ok)' }} />
        <span style={{ width: `${pct(reserved)}%`, background: 'var(--info)' }} />
        <span style={{ width: `${pct(borrowed)}%`, background: 'var(--violet)' }} />
        <span style={{ width: `${pct(other)}%`, background: 'var(--muted-2)' }} />
      </div>
      {showLegend && (
        <div className="legend">
          <span><i style={{ background: 'var(--ok)' }} />Khả dụng {available}</span>
          <span><i style={{ background: 'var(--info)' }} />Đã giữ {reserved}</span>
          <span><i style={{ background: 'var(--violet)' }} />Đang mượn {borrowed}</span>
          {other > 0 && <span><i style={{ background: 'var(--muted-2)' }} />Khác {other}</span>}
        </div>
      )}
    </div>
  );
}
