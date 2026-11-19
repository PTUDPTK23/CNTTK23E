// Kiểm thử đồng thời: N người cùng đặt 1 đầu sách có K bản => đúng K RESERVED, N-K REJECTED.
// Yêu cầu: Node 18+, Library API + Worker đang chạy.   Dùng: node scripts/concurrency-test.mjs [N] [K]
const API = process.env.API_URL ?? 'http://localhost:8081/api';
const N = Number(process.argv[2] ?? 20);
const K = Number(process.argv[3] ?? 3);
const j = async (p, o = {}) => {
  const r = await fetch(API + p, { headers: { 'Content-Type': 'application/json' }, ...o });
  const b = await r.json().catch(() => null);
  if (!r.ok) throw new Error(`${p} -> ${r.status} ${b?.message}`);
  return b.data;
};
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const tag = Date.now();

const cats = await j('/categories');
const book = await j('/books', { method: 'POST', body: JSON.stringify({ title: `Sách thử tải ${tag}`, author: 'Test', categoryId: cats[0].id }) });
for (let i = 0; i < K; i++) await j(`/books/${book.id}/copies`, { method: 'POST', body: '{}' });
console.log(`Đầu sách ${book.id} có ${K} bản. Tạo ${N} người dùng...`);
const users = [];
for (let i = 0; i < N; i++) users.push(await j('/users', { method: 'POST', body: JSON.stringify({ fullName: `Tester ${i}`, email: `t${tag}_${i}@test.local` }) }));

console.log('Gửi đồng thời...');
const created = await Promise.all(users.map((u) => j('/reservations', { method: 'POST', body: JSON.stringify({ userId: u.id, bookId: book.id }) })));

let res = [];
for (let t = 0; t < 60; t++) {
  res = await Promise.all(created.map((c) => j(`/reservations/${c.reservationId}`)));
  if (res.every((r) => r.status !== 'PENDING')) break;
  await sleep(1000);
}
const count = (s) => res.filter((r) => r.status === s).length;
const copies = res.filter((r) => r.status === 'RESERVED').map((r) => r.copyId);
console.log({ PENDING: count('PENDING'), RESERVED: count('RESERVED'), REJECTED: count('REJECTED') });
const ok = count('RESERVED') === Math.min(K, N) && new Set(copies).size === copies.length && count('PENDING') === 0;
console.log(ok ? 'PASS: không đặt trùng bản sao.' : 'FAIL: kết quả không đúng kỳ vọng.');
process.exit(ok ? 0 : 1);
