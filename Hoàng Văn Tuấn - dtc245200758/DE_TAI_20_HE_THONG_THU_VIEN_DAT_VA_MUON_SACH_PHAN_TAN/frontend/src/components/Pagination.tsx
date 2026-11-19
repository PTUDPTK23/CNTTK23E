import { ChevronLeft, ChevronRight } from 'lucide-react';

export function Pagination({ page, totalPages, onChange }: { page: number; totalPages: number; onChange: (p: number) => void }) {
  if (totalPages <= 1) return null;
  return (
    <div className="row center pager">
      <button className="btn btn-outline" disabled={page <= 0} onClick={() => onChange(page - 1)}><ChevronLeft size={16} /> Trước</button>
      <span className="muted">Trang {page + 1} / {totalPages}</span>
      <button className="btn btn-outline" disabled={page >= totalPages - 1} onClick={() => onChange(page + 1)}>Sau <ChevronRight size={16} /></button>
    </div>
  );
}
