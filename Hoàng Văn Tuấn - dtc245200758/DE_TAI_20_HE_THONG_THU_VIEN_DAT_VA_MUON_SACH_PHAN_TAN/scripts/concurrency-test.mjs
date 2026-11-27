// Kiem thu dong thoi: N tai khoan cung dat 1 dau sach co K ban sao.
// Yeu cau: Node 18+, API + Worker + PostgreSQL + RabbitMQ dang chay.
// Chay: $env:ADMIN_TOKEN='JWT_ADMIN'; node scripts/concurrency-test.mjs 100 10
// Script tu tao N tai khoan USER, dang nhap lay JWT, tao sach va ban sao bang ADMIN_TOKEN.
const API = process.env.API_URL ?? 'http://localhost:8081/api';
const ADMIN_TOKEN = process.env.ADMIN_TOKEN;
const N = Number(process.argv[2] ?? 100);
const K = Number(process.argv[3] ?? 10);

if (!ADMIN_TOKEN) {
  throw new Error('Thieu ADMIN_TOKEN. Hay dang nhap tai khoan ADMIN va gan JWT vao bien moi truong.');
}
if (!Number.isInteger(N) || N < 2 || N > 200) {
  throw new Error('N phai la so nguyen tu 2 den 200.');
}
if (!Number.isInteger(K) || K < 1 || K > N) {
  throw new Error('K phai la so nguyen tu 1 den N.');
}

async function api(path, { token, method = 'GET', body } = {}) {
  const headers = { 'Content-Type': 'application/json' };
  if (token) headers.Authorization = `Bearer ${token}`;
  const response = await fetch(`${API}${path}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const payload = await response.json().catch(() => null);
  if (!response.ok) {
    throw new Error(`${method} ${path} -> HTTP ${response.status}: ${payload?.message ?? 'Unknown error'}`);
  }
  return payload?.data;
}

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
const tag = `${Date.now()}-${Math.random().toString(36).slice(2, 7)}`;
const password = `LoadTest#${tag}Aa`;
const categoryList = await api('/categories', { token: ADMIN_TOKEN });
if (!Array.isArray(categoryList) || categoryList.length === 0) {
  throw new Error('Khong co the loai sach de tao du lieu kiem thu.');
}

console.log(`Tao ${N} tai khoan kiem thu...`);
const users = [];
for (let i = 0; i < N; i++) {
  const email = `load-${tag}-${i}@example.test`;
  const registered = await api('/auth/register', {
    method: 'POST',
    body: { fullName: `Load Test ${i + 1}`, email, password },
  });
  const login = await api('/auth/login', {
    method: 'POST',
    body: { email, password },
  });
  users.push({ id: registered.id, token: login.accessToken ?? login.token });
  if (!users[i].token) {
    throw new Error('Khong doc duoc JWT tu API dang nhap. Kiem tra AuthResponse.');
  }
}

console.log('Tao dau sach va cac ban sao...');
const book = await api('/books', {
  token: ADMIN_TOKEN,
  method: 'POST',
  body: {
    title: `Sach kiem thu dong thoi ${tag}`,
    author: 'Load Test',
    categoryId: categoryList[0].id,
  },
});
for (let i = 0; i < K; i++) {
  await api(`/books/${book.id}/copies`, {
    token: ADMIN_TOKEN,
    method: 'POST',
    body: {},
  });
}

console.log(`Gui ${N} yeu cau dong thoi cho ${K} ban sao...`);
const requestStartedAt = Date.now();
const created = await Promise.all(users.map(async (user, index) => {
  const reservation = await api('/reservations', {
    token: user.token,
    method: 'POST',
    body: { userId: user.id, bookId: book.id },
  });
  if (!reservation?.reservationId) {
    throw new Error(`Yeu cau thu ${index + 1} khong tra ve reservationId.`);
  }
  return { id: reservation.reservationId, token: user.token };
}));
const requestDurationMs = Date.now() - requestStartedAt;

let results = [];
let completed = false;
for (let attempt = 0; attempt < 90; attempt++) {
  results = await Promise.all(created.map(({ id, token }) =>
    api(`/reservations/${id}`, { token })));
  if (results.every((item) => item.status !== 'PENDING')) {
    completed = true;
    break;
  }
  await sleep(1000);
}

const count = (status) => results.filter((item) => item.status === status).length;
const reservedCopies = results
  .filter((item) => item.status === 'RESERVED')
  .map((item) => item.copyId);
const duplicateCopies = reservedCopies.length - new Set(reservedCopies).size;
const missingCopyIds = reservedCopies.filter((copyId) => !copyId).length;
const pending = count('PENDING');
const reserved = count('RESERVED');
const rejected = count('REJECTED');

console.log('\n========== KET QUA KIEM THU ==========');
console.table({
  TongYeuCau: N,
  SoBanSao: K,
  RESERVED: reserved,
  REJECTED: rejected,
  PENDING: pending,
  BanSaoBiCapTrung: duplicateCopies,
  RESERVEDThieuCopyId: missingCopyIds,
  ThoiGianGuiYeuCauMs: requestDurationMs,
  DaXuLyHet: completed,
});
const passed =
  completed &&
  results.length === N &&
  reserved === K &&
  rejected === N - K &&
  pending === 0 &&
  duplicateCopies === 0 &&
  missingCopyIds === 0;

console.log(passed
  ? 'PASS: du so luong ban sao, khong cap trung, khong con yeu cau PENDING.'
  : 'FAIL: ket qua chua dat. Hay kiem tra log API, Worker, RabbitMQ va database.');
console.log(`Book ID: ${book.id}`);
console.log('Tai khoan kiem thu duoc tao trong database va duoc giu lai de phuc vu doi chieu.');
process.exitCode = passed ? 0 : 1;
