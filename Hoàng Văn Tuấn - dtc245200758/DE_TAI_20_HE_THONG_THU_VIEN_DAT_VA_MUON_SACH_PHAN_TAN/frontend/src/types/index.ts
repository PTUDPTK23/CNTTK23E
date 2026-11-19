export type ReservationStatus = 'PENDING' | 'RESERVED' | 'REJECTED' | 'BORROWED' | 'RETURNED' | 'CANCELLED';
export type CopyStatus = 'AVAILABLE' | 'RESERVED' | 'BORROWED' | 'MAINTENANCE' | 'LOST';

export interface PageResponse<T> {
  items: T[];
  page: number;
  size: number;
  totalElements: number;
  totalPages: number;
}

export interface Category { id: string; name: string; description: string | null }

export interface Book {
  id: string;
  isbn: string | null;
  title: string;
  author: string;
  publisher: string | null;
  publicationYear: number | null;
  categoryId: string;
  categoryName: string;
  description: string | null;
  coverImage: string | null;
  totalCopies: number;
  availableCopies: number;
  reservedCopies: number;
  borrowedCopies: number;
}

export interface BookInput {
  title: string;
  author: string;
  isbn?: string;
  publisher?: string;
  publicationYear?: number | null;
  categoryId: string;
  description?: string;
  coverImage?: string;
}

export interface Copy { id: string; bookId: string; copyCode: string; status: CopyStatus }

export interface User { id: string; fullName: string; email: string; phone?: string | null; role: 'ADMIN' | 'LIBRARIAN' | 'USER'; status: string; createdAt?: string }

export interface Reservation {
  id: string;
  requestId: string;
  userId: string;
  userName: string;
  bookId: string;
  bookTitle: string;
  bookAuthor: string;
  copyId: string | null;
  copyCode: string | null;
  status: ReservationStatus;
  requestedAt: string;
  processedAt: string | null;
  borrowedAt: string | null;
  dueDate: string | null;
  returnedAt: string | null;
  rejectionReason: string | null;
}

export interface ReservationEvent { id: string; oldStatus: string | null; newStatus: string; note: string | null; actorType: string; createdAt: string }
export interface ReservationCreated { reservationId: string; requestId: string; status: ReservationStatus }

export interface Stats {
  totalBooks: number;
  totalCopies: number;
  availableCopies: number;
  reservedCopies: number;
  borrowedCopies: number;
  pendingReservations: number;
  totalUsers: number;
  overdueLoans: number;
  reservationsByStatus: Record<ReservationStatus, number>;
}

export interface CategoryStat { category: string; totalCopies: number; availableCopies: number }

/** Tình trạng đường ống Outbox -> RabbitMQ -> Worker. */
export interface Pipeline {
  rabbitUp: boolean;
  queueMessages: number;
  consumers: number;
  deadLetterMessages: number;
  outboxPending: number;
  outboxPublished: number;
}

export interface UserInput { fullName: string; email: string; phone?: string; role: 'ADMIN' | 'LIBRARIAN' | 'USER' }


export interface AuthResponse {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  user: User;
}

export interface RegisterInput {
  fullName: string;
  email: string;
  phone?: string;
  password: string;
}

export interface VerifyEmailInput {
  email: string;
  code: string;
}

export interface LoginInput {
  email: string;
  password: string;
}
