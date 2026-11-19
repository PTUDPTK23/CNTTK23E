import { Database, Globe, Layers, Rabbit, Cpu } from 'lucide-react';
import type { ReactNode } from 'react';

interface Node { icon: ReactNode; title: string; sub: string; badge?: string; tone?: 'ok' | 'warn' | 'bad' }

/** Sơ đồ kiến trúc: Client -> Library API -> RabbitMQ -> Reservation Worker -> Database (có hoạt ảnh luồng dữ liệu). */
export function PipelineFlow({ badges, tones }: { badges?: Partial<Record<'api' | 'mq' | 'worker' | 'db', string>>; tones?: Partial<Record<'api' | 'mq' | 'worker' | 'db', 'ok' | 'warn' | 'bad'>> }) {
  const nodes: Node[] = [
    { icon: <Globe size={22} />, title: 'Client', sub: 'React SPA' },
    { icon: <Layers size={22} />, title: 'Library API', sub: 'Spring Boot · REST', badge: badges?.api, tone: tones?.api },
    { icon: <Rabbit size={22} />, title: 'RabbitMQ', sub: 'Exchange · Queue · DLQ', badge: badges?.mq, tone: tones?.mq },
    { icon: <Cpu size={22} />, title: 'Reservation Worker', sub: 'Consumer idempotent', badge: badges?.worker, tone: tones?.worker },
    { icon: <Database size={22} />, title: 'PostgreSQL', sub: 'Dữ liệu dùng chung', badge: badges?.db, tone: tones?.db },
  ];
  return (
    <div className="flow">
      {nodes.map((n, i) => (
        <div key={n.title} className="flow-item">
          <div className="flow-node">
            <div className="flow-icon">{n.icon}</div>
            <b>{n.title}</b>
            <span className="muted small">{n.sub}</span>
            {n.badge && <span className={`pill pill-${n.tone ?? 'ok'}`}>{n.badge}</span>}
          </div>
          {i < nodes.length - 1 && <div className="flow-arrow" aria-hidden><span /></div>}
        </div>
      ))}
    </div>
  );
}
