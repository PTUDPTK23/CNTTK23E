import { Link, Route, Routes } from 'react-router-dom';
import { Layout } from './layouts/Layout';
import AdminPage from './pages/AdminPage';
import BookDetailPage from './pages/BookDetailPage';
import BooksPage from './pages/BooksPage';
import HomePage from './pages/HomePage';
import MyBooksPage from './pages/MyBooksPage';
import ReservePage from './pages/ReservePage';
import TrackPage from './pages/TrackPage';
import { LoginPage, RegisterPage } from './pages/AuthPages';

export default function App() {
  return (
    <Routes>
      <Route path="dang-nhap" element={<LoginPage />} />
      <Route path="dang-ky" element={<RegisterPage />} />

      <Route element={<Layout />}>
        <Route index element={<HomePage />} />
        <Route path="sach" element={<BooksPage />} />
        <Route path="sach/:id" element={<BookDetailPage />} />
        <Route path="dat-muon/:bookId" element={<ReservePage />} />
        <Route path="yeu-cau" element={<TrackPage />} />
        <Route path="yeu-cau/:id" element={<TrackPage />} />
        <Route path="sach-cua-toi" element={<MyBooksPage />} />
        <Route path="quan-tri" element={<AdminPage />} />
        <Route path="*" element={<div className="state empty">Không tìm thấy trang. <Link to="/">Về trang chủ</Link></div>} />
      </Route>
    </Routes>
  );
}