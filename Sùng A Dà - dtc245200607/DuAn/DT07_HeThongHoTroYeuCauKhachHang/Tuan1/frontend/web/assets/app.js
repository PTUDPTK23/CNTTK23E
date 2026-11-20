const API = window.SUPPORT_API_URL || `${window.location.origin}/api`;
const NOTIFICATIONS = window.SUPPORT_NOTIFICATION_URL || `${window.location.origin}/notifications`;
const $ = (id) => document.getElementById(id);
let tickets = [];
let currentUser = null;
let refreshTimer;
let notificationSocket = null;
let notificationRetryTimer = null;
let notificationPingTimer = null;
const statusText = {New:'Mới tiếp nhận',Assigned:'Đã phân công',InProgress:'Đang xử lý',Resolved:'Đã hoàn thành'};
const safe = (v='') => String(v).replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));

async function request(path, options={}) {
  const headers = {'Content-Type':'application/json', ...(options.headers||{})};
  const token = sessionStorage.getItem('supportToken');
  if (token) headers.Authorization = `Bearer ${token}`;
  const response = await fetch(API + path, {...options, headers});
  const body = await response.text();
  if (response.status === 401 && path !== '/auth/login') logout();
  if (!response.ok) throw new Error(body || `HTTP ${response.status}`);
  return body ? JSON.parse(body) : null;
}
function render() {
  const q = $('searchInput').value.trim().toLowerCase();
  const filter = $('statusFilter').value;
  const visible = tickets.filter(t => (!filter || t.status===filter) && (!q || `${t.id} ${t.requesterId} ${t.content}`.toLowerCase().includes(q)));
  $('ticketRows').innerHTML = visible.length ? visible.map(t => `<tr>
    <td title="${safe(t.id)}">#${safe(String(t.id).slice(0,8))}</td><td>${safe(t.requesterId)}</td><td>${safe(t.content)}</td>
    <td class="${t.priority==='High'?'priority-high':''}">${t.priority==='High'?'Cao':'Bình thường'}</td>
    <td><span class="status status-${safe(t.status)}">${safe(statusText[t.status]||t.status)}</span></td>
    <td>${currentUser.role!=='Requester' && t.status!=='Resolved'?`<button class="ghost-btn" data-id="${safe(t.id)}" data-status="${safe(t.status)}">Cập nhật</button>`:'—'}</td>
  </tr>`).join('') : '<tr><td colspan="6" class="empty">Chưa có yêu cầu phù hợp.</td></tr>';
  $('totalCount').textContent=tickets.length;
  $('newCount').textContent=tickets.filter(t=>t.status==='New').length;
  $('workingCount').textContent=tickets.filter(t=>['Assigned','InProgress'].includes(t.status)).length;
  $('doneCount').textContent=tickets.filter(t=>t.status==='Resolved').length;
}
async function loadTickets() {
  if (!currentUser) return;
  $('listMessage').textContent='Đang tải dữ liệu...';
  try { tickets=await request('/tickets') || []; render(); $('listMessage').textContent=`Hiển thị ${tickets.length} yêu cầu.`; }
  catch(e) { $('listMessage').textContent='Không tải được dữ liệu: '+e.message; }
}
function showSession() {
  $('sessionLabel').textContent=`${currentUser.username} · ${currentUser.role}`;
  $('loginPanel').hidden=true; $('mainContent').hidden=false; $('logoutBtn').hidden=false;
  $('requesterId').value=currentUser.username; $('requesterId').readOnly=currentUser.role==='Requester';
  loadTickets(); disconnectNotifications(); connectNotifications();
  clearInterval(refreshTimer); refreshTimer=setInterval(loadTickets,15000);
}
function logout() {
  sessionStorage.removeItem('supportToken'); sessionStorage.removeItem('supportUser'); currentUser=null;
  $('loginPanel').hidden=false; $('mainContent').hidden=true; $('logoutBtn').hidden=true; $('sessionLabel').textContent='Chưa đăng nhập';
  clearInterval(refreshTimer);
  disconnectNotifications();
}
function disconnectNotifications() {
  clearTimeout(notificationRetryTimer); notificationRetryTimer=null;
  clearInterval(notificationPingTimer); notificationPingTimer=null;
  if (notificationSocket) {
    const socket=notificationSocket; notificationSocket=null;
    socket.onclose=null; socket.close();
  }
}
async function connectNotifications() {
  if (!currentUser || (notificationSocket && notificationSocket.readyState < WebSocket.CLOSING)) return;
  try {
    const token=sessionStorage.getItem('supportToken');
    const response=await fetch(`${NOTIFICATIONS}/negotiate?negotiateVersion=1`,{method:'POST',headers:{Authorization:`Bearer ${token}`}});
    if(!response.ok) throw new Error('Negotiate failed');
    const data=await response.json();
    if(!currentUser || !sessionStorage.getItem('supportToken')) return;
    const wsUrl=new URL(NOTIFICATIONS); wsUrl.protocol=wsUrl.protocol==='https:'?'wss:':'ws:'; wsUrl.searchParams.set('id',data.connectionToken); wsUrl.searchParams.set('access_token',token);
    const socket=new WebSocket(wsUrl);
    let handshaken=false;
    notificationSocket=socket;
    socket.onopen=()=>socket.send('{"protocol":"json","version":1}\u001e');
    socket.onmessage=(event)=>{
      for(const frame of String(event.data).split('\u001e').filter(Boolean)) {
        try {
          const message=JSON.parse(frame);
          if(!handshaken) {
            if(message.error) { socket.close(); return; }
            handshaken=true;
            notificationPingTimer=setInterval(()=>{if(socket.readyState===WebSocket.OPEN)socket.send('{"type":6}\u001e');},15000);
            continue;
          }
          if(message.type===1 && message.target==='TicketUpdated') loadTickets();
        }
        catch { /* Handshake response is not an application notification. */ }
      }
    };
    socket.onclose=()=>{
      clearInterval(notificationPingTimer); notificationPingTimer=null;
      if(notificationSocket===socket) notificationSocket=null;
      if(currentUser) notificationRetryTimer=setTimeout(connectNotifications,5000);
    };
  } catch {
    if(currentUser) notificationRetryTimer=setTimeout(connectNotifications,5000);
  }
}

$('loginForm').addEventListener('submit',async e=>{
  e.preventDefault(); $('loginMessage').textContent='Đang đăng nhập...';
  try {
    const result=await request('/auth/login',{method:'POST',body:JSON.stringify({username:$('username').value.trim(),password:$('password').value})});
    sessionStorage.setItem('supportToken',result.token); currentUser={username:result.username,role:result.role};
    sessionStorage.setItem('supportUser',JSON.stringify(currentUser)); $('password').value=''; $('loginMessage').textContent=''; showSession();
  } catch { $('loginMessage').textContent='Tên đăng nhập hoặc mật khẩu không đúng.'; }
});
$('logoutBtn').addEventListener('click',logout);
$('ticketForm').addEventListener('submit',async e=>{
  e.preventDefault(); const msg=$('formMessage'); msg.textContent='Đang gửi...';
  try { await request('/tickets',{method:'POST',body:JSON.stringify({requesterId:$('requesterId').value.trim(),content:$('content').value.trim(),priority:$('priority').value})}); msg.textContent='✓ Gửi yêu cầu thành công!'; $('content').value=''; await loadTickets(); }
  catch(err) { msg.textContent='Không gửi được: '+err.message; }
});
$('refreshBtn').addEventListener('click',loadTickets);
$('searchInput').addEventListener('input',render);
$('statusFilter').addEventListener('change',render);
$('ticketRows').addEventListener('click',async e=>{
  const btn=e.target.closest('button[data-id]'); if(!btn)return;
  const next={New:'InProgress',Assigned:'InProgress',InProgress:'Resolved'}[btn.dataset.status]; if(!next)return;
  if(!confirm(`Chuyển yêu cầu sang "${statusText[next]}"?`))return;
  try { await request(`/tickets/${btn.dataset.id}/status`,{method:'PATCH',body:JSON.stringify({status:next})}); await loadTickets(); }
  catch(err){alert('Cập nhật thất bại: '+err.message);}
});
try { const saved=JSON.parse(sessionStorage.getItem('supportUser')); if(sessionStorage.getItem('supportToken')&&saved){currentUser=saved;showSession();} } catch { logout(); }
