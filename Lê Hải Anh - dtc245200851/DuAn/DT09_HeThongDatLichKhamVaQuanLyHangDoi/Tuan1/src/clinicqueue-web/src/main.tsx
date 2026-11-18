import { StrictMode, useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import './style.css';
function App() {
 const [status, setStatus] = useState('Đang kiểm tra');
 const [checking, setChecking] = useState(false);
 async function check() {
  setChecking(true);
  try {
   const results = await Promise.all(['/health/api', '/health/notification'].map(path => fetch(path, {signal: AbortSignal.timeout(5000)})));
   setStatus(results.every(r => r.ok) ? 'Dịch vụ sẵn sàng' : 'Có dịch vụ chưa sẵn sàng');
  } catch { setStatus('Chưa kết nối được dịch vụ'); }
  finally { setChecking(false); }
 }
 useEffect(() => { void check(); }, []);
 return <div className="shell"><header><a href="/" className="brand"><span className="mark">✚</span>ClinicQueue</a><span className="phase">KHỞI ĐẦU MỚI · M0</span></header>
 <main><section className="hero"><div><p className="eyebrow">ÍT CHỜ ĐỢI HƠN. AN TÂM NHIỀU HƠN.</p><h1>Mỗi lượt khám,<br/><em>một sự quan tâm.</em></h1><p className="intro">Một nơi để kết nối lịch khám và hàng đợi. ClinicQueue đang được chuẩn bị để hành trình chăm sóc sức khỏe của bạn nhẹ nhàng hơn.</p><a className="button" href="#readiness">Khám phá hệ thống <span>↗</span></a><div className="note"><span>◷</span> Thời gian của bạn luôn được trân trọng.</div></div>
 <div className="illustration" aria-label="Minh họa quy trình khám bệnh"><div className="orbit"></div><div className="float-label">✦ Chăm sóc có kết nối</div><div className="ticket"><div className="ticket-top"><span>CLINICQUEUE</span><span>✚</span></div><p>HÀNH TRÌNH CHĂM SÓC</p><div className="ticket-number">01<span> / Khởi đầu</span></div><div className="dash"></div><div className="steps"><span>◉ Lên lịch</span><span>◉ Theo dõi lượt</span><span>◉ An tâm khám</span></div><small>Minh họa trải nghiệm · Chưa mở đặt lịch</small></div><div className="small-card"><span className="green-dot"></span> Sẵn sàng cho một trải nghiệm mới</div></div></section>
 <section id="readiness" className="readiness"><div><p className="eyebrow">NỀN TẢNG ĐANG ĐƯỢC CHUẨN BỊ</p><h2>Một khởi đầu vững vàng.</h2></div><div className="status" data-state={checking ? "checking" : status === "Dịch vụ sẵn sàng" ? "ready" : "unavailable"} role="status"><span className="green-dot"></span>{status}<button disabled={checking} onClick={() => void check()} aria-label="Kiểm tra lại dịch vụ">{checking ? '…' : '↻'}</button></div></section>
 <div className="cards">{[['01','Lịch khám thuận tiện','Tìm khung giờ phù hợp, chủ động kế hoạch chăm sóc.'],['02','Hàng đợi rõ ràng','Theo dõi lượt khám để dành thời gian cho điều quan trọng.'],['03','Kết nối liền mạch','Thông tin được kết nối giữa phòng khám và người bệnh.']].map(([n,title,desc]) => <article key={n}><span className="card-no">{n} /</span><h3>{title}</h3><p>{desc}</p><span className="coming">SẮP CÓ TRONG CÁC GIAI ĐOẠN TIẾP THEO</span></article>)}</div>
 </main><footer><span>© {new Date().getFullYear()} ClinicQueue</span><span>Được xây dựng cho những lượt khám an tâm.</span></footer></div>;
}
createRoot(document.getElementById('root')!).render(<StrictMode><App/></StrictMode>);
