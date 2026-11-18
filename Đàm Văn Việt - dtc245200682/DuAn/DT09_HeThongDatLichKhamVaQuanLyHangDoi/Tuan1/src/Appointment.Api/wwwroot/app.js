'use strict';
const $ = id => document.getElementById(id);
const staff = document.body.dataset.page === 'staff';
const labels = { Pending:'Chờ cấp số', Waiting:'Đang chờ khám', Serving:'Đến lượt khám của bạn', Completed:'Đã hoàn thành', Cancelled:'Đã hủy', Expired:'Đã hết hạn' };
let connection, selectedSlot, queueGroup, privateGroup, config, staffToken = sessionStorage.getItem('staffToken'), loading = false;
let activeAppointment = null;
let queueRequest=0, slotRequest=0, appointmentRequest=0;
let queueSubscription=Promise.resolve();
function stored(key) { try { return JSON.parse(sessionStorage.getItem(key) || 'null'); } catch { sessionStorage.removeItem(key); return null; } }
let pendingBooking=stored('pendingBooking');
const dateTime = value => new Date(value).toLocaleString('vi-VN', { timeZone:'Asia/Ho_Chi_Minh',hour:'2-digit',minute:'2-digit',day:'2-digit',month:'2-digit' });
const hour = value => new Date(value).toLocaleTimeString('vi-VN', { timeZone:'Asia/Ho_Chi_Minh',hour:'2-digit',minute:'2-digit' });
function notice(message, error=false) { $('notice').hidden = false; $('notice').textContent = message; $('notice').className = 'notice' + (error ? ' error' : ''); }
function safely(action) { return async event => { event?.preventDefault?.(); try { await action(event); } catch (e) { notice(e.message,true); } }; }
async function request(path, options={}) {
  const response = await fetch(path, { ...options, headers:{ 'Content-Type':'application/json', ...(options.headers || {}) } });
  if (!response.ok) {
    let body; try { body=await response.json(); } catch { /* empty auth response */ }
    const error=new Error(body?.error || ({401:'Vui lòng đăng nhập nhân viên.',403:'Mã hoặc khóa truy cập không đúng.',429:'Đã thử đăng nhập quá nhiều lần. Vui lòng chờ một phút.'}[response.status]) || `Không xử lý được yêu cầu (${response.status}).`);
    error.status=response.status;throw error;
  }
  return response.status===204 ? null : response.json();
}
async function showQueue() {
  if (!$('doctor').value || !$('date').value) return;
  const version=++queueRequest;
  const key = `${$('doctor').value}:${$('date').value}`;
  const q = await request(`/queues/${$('doctor').value}?date=${$('date').value}`);
  if (version!==queueRequest || key !== `${$('doctor').value}:${$('date').value}`) return;
  $('current').textContent = q.currentNumber ?? '—';
  $('waitingCount').textContent = q.waiting.length;
  $('queueCaption').textContent = `${$('doctor').selectedOptions[0]?.textContent} · ${q.date}`;
  $('waiting').replaceChildren();
  for (const ticket of q.waiting) {
    const span = document.createElement('span'); span.className='ticket'; span.textContent=ticket.number; span.title=`Khung giờ từ ${hour(ticket.startsAt)}`; $('waiting').append(span);
  }
  if (!q.waiting.length) { const p=document.createElement('p');p.className='muted';p.textContent='Chưa có lượt đang chờ.';$('waiting').append(p); }
}
async function slots() {
  if (staff) return;
  const version=++slotRequest;
  const key = `${$('doctor').value}:${$('date').value}`;
  const list=await request(`/doctors/${$('doctor').value}/slots?date=${$('date').value}`);
  if(version!==slotRequest || key!==`${$('doctor').value}:${$('date').value}`) return;
  if(!list.some(s=>s.id===selectedSlot && s.available>0)) selectedSlot=null;
  $('slots').replaceChildren();
  for (const slot of list) {
    const button=document.createElement('button'); button.type='button'; button.className='slot'; button.dataset.slotId=slot.id;
    button.append(document.createTextNode(`${hour(slot.startsAt)} – ${hour(slot.endsAt)}`));
    const detail=document.createElement('small'); detail.textContent=`Còn ${slot.available}/${slot.capacity} chỗ`; button.append(detail); button.disabled=slot.available===0;
    if(slot.id===selectedSlot) button.classList.add('selected');
    button.onclick=()=> { document.querySelectorAll('.slot').forEach(b=>b.classList.remove('selected')); button.classList.add('selected');selectedSlot=slot.id; };
    $('slots').append(button);
  }
  if (!list.length) $('slots').textContent='Không có khung giờ khả dụng trong ngày này.';
}
async function subscribeQueue() {
  queueSubscription=queueSubscription.catch(()=>{}).then(updateQueueSubscription);
  return queueSubscription;
}
async function updateQueueSubscription() {
  if(connection?.state!=='Connected') return;
  const next={doctor:Number($('doctor').value),date:$('date').value};
  if(queueGroup) await connection.invoke('LeaveQueue',queueGroup.doctor,queueGroup.date);
  await connection.invoke('JoinQueue',next.doctor,next.date);queueGroup=next;
}
async function subscribeAppointment() {
  if (staff || connection?.state!=='Connected') return;
  if(privateGroup) { await connection.invoke('LeaveAppointment',privateGroup);privateGroup=null; }
  if(activeAppointment) {
    await connection.invoke('JoinAppointment',activeAppointment.id,activeAppointment.token);
    privateGroup=activeAppointment.id;
  }
}
async function showAppointment() {
  if (staff || !activeAppointment) return;
  const id=activeAppointment.id;
  const version=++appointmentRequest;
  const appointment=await request(`/appointments/${id}`,{headers:{'X-Appointment-Token':activeAppointment.token}});
  if(version!==appointmentRequest || id!==activeAppointment?.id) return;
  $('appointment').hidden=false; $('myNumber').textContent=appointment.number ?? '—'; $('myStatus').textContent=labels[appointment.status];
  $('myStatus').dataset.status=appointment.status;
  $('myName').textContent=appointment.patientName;
  $('myTime').textContent=`${dateTime(appointment.startsAt)} – ${hour(appointment.endsAt)}`;
  $('cancel').disabled=!['Pending','Waiting'].includes(appointment.status);
}
async function chooseQueue() { selectedSlot=null; await subscribeQueue(); await Promise.all([showQueue(),slots()]); }
async function restoreConnection() {
  queueGroup=null;privateGroup=null;
  await subscribeQueue();
  try { await subscribeAppointment(); await showAppointment(); }
  catch(e) { notice(e.message,true); }
  await Promise.all([showQueue(),slots()]);
  $('connection').textContent='● Đang cập nhật trực tiếp';$('connection').className='connection live';
  $('connection').dataset.state='connected';
}
async function startConnection() {
  try { if(connection.state==='Disconnected') await connection.start(); if(connection.state==='Connected') await restoreConnection(); }
  catch { $('connection').textContent='Mất kết nối · đang thử lại…';$('connection').className='connection';$('connection').dataset.state='disconnected';setTimeout(startConnection,3000); }
}
function staffSession() {
  $('login').hidden=!!staffToken;$('staffSession').hidden=!staffToken;
  $('next').disabled=!staffToken;$('complete').disabled=!staffToken;
}
function bookingSession() { $('retryBooking').hidden=!pendingBooking; }
async function submitBooking() {
  if(loading || !pendingBooking) return;
  loading=true;$('book').disabled=true;$('retryBooking').disabled=true;
  let booked=false;
  try {
    const a=await request('/appointments',{method:'POST',headers:{'Idempotency-Key':pendingBooking.key},body:JSON.stringify(pendingBooking.body)});
    activeAppointment={id:a.id,token:a.accessToken};sessionStorage.setItem('appointment',JSON.stringify(activeAppointment));
    pendingBooking=null;sessionStorage.removeItem('pendingBooking');booked=true;
    $('appointmentId').value=a.id;$('appointmentToken').value=a.accessToken;
    const found=await request(`/appointments/${a.id}`,{headers:{'X-Appointment-Token':a.accessToken}});
    $('doctor').value=found.doctorId;$('date').value=found.date;
    await subscribeQueue();await subscribeAppointment();
    await Promise.all([showAppointment(),showQueue(),slots()]);
    notice(`Đã xác nhận lịch hẹn: ${a.id}. Hãy lưu mã và khóa truy cập.`);
  } catch(e) {
    if(booked) notice('Lịch đã được lưu. Kết nối cập nhật đang gián đoạn; hãy dùng Tra cứu để xem lại.',true);
    else {
      if(e.status>=400 && e.status<500) { pendingBooking=null;sessionStorage.removeItem('pendingBooking'); }
      notice(pendingBooking?'Chưa nhận được kết quả đặt lịch. Bấm “Thử lại yêu cầu trước” để xác nhận, tránh đặt trùng.':e.message,true);
    }
  } finally { loading=false;$('book').disabled=false;$('retryBooking').disabled=false;bookingSession(); }
}
async function init() {
  config=await request('/client-config');$('date').value=config.today;
  const doctors=await request('/doctors');
  for(const d of doctors) {const option=document.createElement('option');option.value=d.id;option.textContent=`${d.name} · ${d.specialty}`;$('doctor').append(option);}
  $('doctor').onchange=safely(chooseQueue);$('date').onchange=safely(chooseQueue);
  $('refresh').onclick=safely(async()=>{await Promise.all([showQueue(),showAppointment(),slots()]);notice('Đã lấy trạng thái mới nhất.');});
  if(staff) {
    staffSession();
    $('login').onsubmit=safely(async()=> { const r=await request('/staff/login',{method:'POST',body:JSON.stringify({password:$('staffPassword').value})});staffToken=r.token;sessionStorage.setItem('staffToken',staffToken);$('staffPassword').value='';staffSession();notice('Đăng nhập thành công.'); });
    $('logout').onclick=()=>{staffToken=null;sessionStorage.removeItem('staffToken');staffSession();};
    for(const action of ['next','complete']) $(action).onclick=safely(async()=>{
      $(action).disabled=true;
      try {await request(`/internal/queues/${$('doctor').value}/${action}?date=${$('date').value}`,{method:'POST',headers:{Authorization:`Bearer ${staffToken}`}});await showQueue();notice(action==='next'?'Đã gọi lượt tiếp theo.':'Đã hoàn thành lượt khám.');}
      finally {staffSession();}
    });
  } else {
    $('date').min=config.today;
    const saved=stored('appointment');
    if(saved) {
      activeAppointment=saved;$('appointmentId').value=saved.id;$('appointmentToken').value=saved.token;
      try {
        const found=await request(`/appointments/${saved.id}`,{headers:{'X-Appointment-Token':saved.token}});
        $('doctor').value=found.doctorId;$('date').value=found.date;await showAppointment();
      } catch(e) { notice(e.message,true); }
    }
    bookingSession();$('retryBooking').onclick=safely(submitBooking);
    $('booking').onsubmit=safely(async()=>{
      if(pendingBooking) { await submitBooking();return; }
      if(!selectedSlot) throw new Error('Hãy chọn một khung giờ còn chỗ.');
      const bytes=crypto.getRandomValues(new Uint8Array(32));
      pendingBooking={key:Array.from(bytes,b=>b.toString(16).padStart(2,'0')).join(''),body:{slotId:selectedSlot,patientName:$('patientName').value,phone:$('phone').value}};
      sessionStorage.setItem('pendingBooking',JSON.stringify(pendingBooking));
      await submitBooking();
    });
    $('lookup').onsubmit=safely(async()=>{
      const id=$('appointmentId').value.trim(),token=$('appointmentToken').value.trim();
      // Verify ownership before replacing the current appointment subscription.
      const found=await request(`/appointments/${id}`,{headers:{'X-Appointment-Token':token}});
      activeAppointment={id,token};sessionStorage.setItem('appointment',JSON.stringify(activeAppointment));
      $('doctor').value=found.doctorId;$('date').value=found.date;await chooseQueue();
      await subscribeAppointment();await showAppointment();notice('Đã tra cứu lịch hẹn.');
    });
    $('cancel').onclick=safely(async()=>{await request(`/appointments/${activeAppointment.id}`,{method:'DELETE',headers:{'X-Appointment-Token':activeAppointment.token}});await Promise.all([showAppointment(),slots(),showQueue()]);notice('Đã hủy lịch và trả chỗ cho khung giờ.');});
    $('copy').onclick=safely(async()=>{await navigator.clipboard.writeText(`Mã: ${activeAppointment.id}\nKhóa: ${activeAppointment.token}`);notice('Đã sao chép mã và khóa.');});
  }
  await chooseQueue();
  connection=new signalR.HubConnectionBuilder().withUrl(`${config.notificationUrl}/hubs/queue`).withAutomaticReconnect([0,1000,3000,5000]).configureLogging(signalR.LogLevel.Warning).build();
  connection.on('QueueChanged',safely(async(event)=>{
    if(event.doctorId===Number($('doctor').value) && event.date===$('date').value) await Promise.all([showQueue(),slots()]);
  }));
  connection.on('AppointmentChanged',safely(showAppointment));
  connection.onreconnecting(()=>{$('connection').textContent='Đang kết nối lại…';$('connection').className='connection';$('connection').dataset.state='reconnecting';});
  connection.onreconnected(safely(restoreConnection));
  connection.onclose(()=>startConnection());
  await startConnection();
}
init().catch(e=>notice(e.message,true));
