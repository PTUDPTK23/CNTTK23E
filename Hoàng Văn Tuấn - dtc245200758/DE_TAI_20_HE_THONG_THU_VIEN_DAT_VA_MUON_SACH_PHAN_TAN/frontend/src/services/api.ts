import type {
  AuthResponse, Book, BookInput, Category, CategoryStat, Copy, Pipeline,
  UserInput, CopyStatus, PageResponse, Reservation, ReservationCreated,
  ReservationEvent, ReservationStatus, Stats, User, RegisterInput,
  VerifyEmailInput, LoginInput,
} from '../types';

const BASE = (import.meta.env.VITE_API_URL as string | undefined) ?? 'http://localhost:8081/api';
export const TOKEN_KEY = 'library.accessToken';

export function saveAccessToken(token: string) {
  localStorage.setItem(TOKEN_KEY, token);
}

export function clearAccessToken() {
  localStorage.removeItem(TOKEN_KEY);
}

export class ApiError extends Error {
  status: number;
  fieldErrors?: Record<string, string>;

  constructor(message: string, status: number, fieldErrors?: Record<string, string>) {
    super(message);
    this.status = status;
    this.fieldErrors = fieldErrors;
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let res: Response;

  const headers = new Headers(init?.headers);
  if (!headers.has('Content-Type')) headers.set('Content-Type', 'application/json');

  const token = localStorage.getItem(TOKEN_KEY);
  if (token && !headers.has('Authorization')) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  try {
    res = await fetch(BASE + path, { ...init, headers });
  } catch {
    throw new ApiError('Không thể kết nối tới máy chủ. Hãy kiểm tra Library API ở cổng 8081.', 0);
  }

  const body = await res.json().catch(() => null);

  if (!res.ok) {
    const fields =
      body && typeof body.data === 'object' && body.data !== null
        ? (body.data as Record<string, string>)
        : undefined;

    throw new ApiError(body?.message ?? `Lỗi máy chủ (${res.status})`, res.status, fields);
  }

  return (body?.data ?? undefined) as T;
}

function qs(params: Record<string, string | number | undefined | null>): string {
  const sp = new URLSearchParams();
  Object.entries(params).forEach(([k, v]) => {
    if (v !== undefined && v !== null && v !== '') sp.set(k, String(v));
  });
  const s = sp.toString();
  return s ? `?${s}` : '';
}

const post = <T,>(path: string, body?: unknown) =>
  request<T>(path, {
    method: 'POST',
    body: body === undefined ? '{}' : JSON.stringify(body),
  });

export const api = {
  register: (data: RegisterInput) => post<User>('/auth/register', data),
  verifyEmail: (data: VerifyEmailInput) => post<User>('/auth/verify-email', data),
  resendVerification: (email: string) => post<string>('/auth/resend-code', { email }),
  login: (data: LoginInput) => post<AuthResponse>('/auth/login', data),
  me: () => request<User>('/auth/me'),

  stats: () => request<Stats>('/stats'),
  statsByCategory: () => request<CategoryStat[]>('/stats/categories'),
  pipeline: () => request<Pipeline>('/system/pipeline'),
  categories: () => request<Category[]>('/categories'),
  createCategory: (c: { name: string; description?: string }) => post<Category>('/categories', c),
  updateCategory: (id: string, c: { name: string; description?: string }) =>
    request<Category>(`/categories/${id}`, { method: 'PUT', body: JSON.stringify(c) }),
  users: async () => (await request<PageResponse<User>>('/users?size=200')).items,
  createUser: (u: UserInput) => post<User>('/users', u),
  setUserStatus: (id: string, status: 'ACTIVE' | 'LOCKED') =>
    request<User>(`/users/${id}/status`, { method: 'PUT', body: JSON.stringify({ status }) }),

  books: (p: { page?: number; size?: number; keyword?: string; categoryId?: string; author?: string; availability?: string }) =>
    request<PageResponse<Book>>('/books' + qs(p)),
  book: (id: string) => request<Book>(`/books/${id}`),
  createBook: (b: BookInput) => post<Book>('/books', b),
  updateBook: (id: string, b: BookInput) => request<Book>(`/books/${id}`, { method: 'PUT', body: JSON.stringify(b) }),
  deleteBook: (id: string) => request<void>(`/books/${id}`, { method: 'DELETE' }),

  copies: (bookId: string) => request<Copy[]>(`/books/${bookId}/copies`),
  addCopy: (bookId: string) => post<Copy>(`/books/${bookId}/copies`, {}),
  setCopyStatus: (copyId: string, status: CopyStatus) =>
    request<Copy>(`/copies/${copyId}/status`, { method: 'PUT', body: JSON.stringify({ status }) }),

  createReservation: (userId: string, bookId: string) =>
    post<ReservationCreated>('/reservations', { userId, bookId }),
  reservation: (id: string) => request<Reservation>(`/reservations/${id}`),
  reservationEvents: (id: string) => request<ReservationEvent[]>(`/reservations/${id}/events`),
  userReservations: async (userId: string) =>
    (await request<PageResponse<Reservation>>(`/users/${userId}/reservations?size=200`)).items,
  allReservations: async (status?: ReservationStatus) =>
    (await request<PageResponse<Reservation>>('/reservations' + qs({ status, size: 100 }))).items,
  cancel: (id: string) => post<Reservation>(`/reservations/${id}/cancel`),
  borrow: (id: string) => post<Reservation>(`/reservations/${id}/borrow`),
  giveBack: (id: string) => post<Reservation>(`/reservations/${id}/return`),
};
