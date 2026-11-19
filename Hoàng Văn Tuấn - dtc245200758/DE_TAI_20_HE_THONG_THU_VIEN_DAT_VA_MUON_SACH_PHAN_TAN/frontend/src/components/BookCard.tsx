import { Link } from 'react-router-dom';
import type { Book } from '../types';
import { AvailabilityBar } from './AvailabilityBar';
import { BookCover } from './BookCover';

export function BookCard({ book }: { book: Book }) {
  const ok = book.availableCopies > 0;
  return (
    <Link to={`/sach/${book.id}`} className="book-card">
      <div className="book-cover-wrap">
        <BookCover title={book.title} author={book.author} src={book.coverImage} />
        <span className={ok ? 'pill pill-ok' : 'pill pill-bad'}>{ok ? 'Còn sách' : 'Hết bản'}</span>
      </div>
      <div className="book-info">
        <span className="chip">{book.categoryName}</span>
        <h3>{book.title}</h3>
        <p className="muted small">{book.author}{book.publicationYear ? ` · ${book.publicationYear}` : ''}</p>
        <AvailabilityBar total={book.totalCopies} available={book.availableCopies} reserved={book.reservedCopies} borrowed={book.borrowedCopies} />
        <p className={ok ? 'avail ok' : 'avail none'}>
          {book.totalCopies === 0 ? 'Chưa có bản sao' : ok ? `${book.availableCopies}/${book.totalCopies} bản khả dụng` : `0/${book.totalCopies} bản khả dụng`}
        </p>
      </div>
    </Link>
  );
}

/** Hàng sách dạng danh sách (chế độ xem list). */
export function BookRow({ book }: { book: Book }) {
  const ok = book.availableCopies > 0;
  return (
    <Link to={`/sach/${book.id}`} className="book-row">
      <BookCover title={book.title} src={book.coverImage} className="row-cover" />
      <div className="grow">
        <h3>{book.title}</h3>
        <p className="muted small">{book.author} · {book.categoryName}{book.publisher ? ` · ${book.publisher}` : ''}</p>
        <p className="small clamp-2">{book.description}</p>
      </div>
      <div className="book-row-side">
        <span className={ok ? 'pill pill-ok' : 'pill pill-bad'}>{ok ? `Còn ${book.availableCopies}/${book.totalCopies}` : 'Hết bản'}</span>
      </div>
    </Link>
  );
}
